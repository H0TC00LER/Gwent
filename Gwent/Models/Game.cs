﻿using Models.Dtos;
using Models.Dtos.GameStartResponse;
using Models.Dtos.MoveResult;

namespace Models;

public class Game
{
    public readonly Player[] Players;

    public Game(string player1Name, string player2Name) //Server creates game here 
    {
        var player1 = new Player(player1Name, this, 1,0);
        var player2 = new Player(player2Name, this, 1,1);
        Players = new[] {player1, player2};
        CurrentlyMoving = player1;
    }

    public Game(GameStartResponse startInfo) //Client creates game here
    {
        Player player1;
        Player player2;
        if (startInfo.ThisPlayerNumber == 0)
        {
            player1 = new Player(startInfo.Player1Name, this, startInfo.Hand,0);
            player2 = new Player(startInfo.Player2Name, this, Array.Empty<byte>(),1);
        }
        else
        {
            player1 = new Player(startInfo.Player1Name, this, Array.Empty<byte>(),0);
            player2 = new Player(startInfo.Player2Name, this, startInfo.Hand,1);
        }

        Players = new[] {player1, player2};
        CurrentlyMoving = player1;
    }

    public List<GameStartResponse> ProvideGameStartResponses()
    {
        var res = new List<GameStartResponse>
        {
            new(Players[0].Name, Players[1].Name,
                0, Players[0].Hand.Select(card => card.Id).ToArray()),
            new(Players[0].Name, Players[1].Name,
                1, Players[1].Hand.Select(card => card.Id).ToArray())
        };
        return res;
    }


    public Player CurrentlyMoving { get; set; }
    public bool IsRoundFinished => Players.All(p => p.HasPassed = true);
    public bool IsGameFinished => Players.Any(player => player.Lives == 0);

    public void SetupNextRound()
    {
        foreach (var player in Players)
        {
            player.HasPassed = false;
            player.OwnField = Player.SetupField();
        }
    }

    //this is move execution on client
    public Game ExecuteMove(MoveResult move)
    {
        UpdateDeck(move);
        ExecuteMove(new PlayerMove(move.PlayerId, move.HasPassed, move.CardPositionInHand, move.Row,
            move.CardPositionInRow));
        return this;
    }

    //this is move execution on server
    public List<MoveResult> ExecuteMove(PlayerMove move)
    {
        var results = new MoveResult[2];
        if (CurrentlyMoving != Players[move.PlayerId]) return results.ToList();
        var moveResult = move.HasPassed
            ? CurrentlyMoving.Pass()
            : CurrentlyMoving.PlayCard(move.CardPositionInHand, move.Row, move.CardPositionInRow);
        if(!IsRoundFinished) CurrentlyMoving = CalculateNextMovingPlayer();
        for (var i = 0; i < Players.Length; i++)
        {
            if (i != moveResult.PlayerId) continue;
            results[i] = moveResult;
            moveResult.PulledCards = new List<byte>();
            results[(i + 1) % 2] = moveResult;
        }

        return results.ToList();
    }

    private void UpdateDeck(MoveResult move)
    {
        Players.First(player => player.Id == move.PlayerId).Deck = new Deck(move.PulledCards, false);
    }

    private Player CalculateNextMovingPlayer()
    {
        if (Players.All(player => player.HasPassed)) throw new Exception("There is no next player in a finished round");
        var otherPlayer = Players.FirstOrDefault(player => player != CurrentlyMoving && !player.HasPassed);
        return otherPlayer ?? CurrentlyMoving;
    }

    public RoundResult CalculateRoundResult()
    {
        var player1 = Players[0];
        var player2 = Players[1];
        RoundResult result;
        if (player1.Power > player2.Power)
        {
            result = new RoundResult(false, player1.Name);
            player2.Lives -= 1;
        }
        else if (player1.Power < player2.Power)
        {
            result = new RoundResult(false, player1.Name);
            player1.Lives -= 1;
        }
        else
        {
            result = new RoundResult(true, null);
            player1.Lives -= 1;
            player2.Lives -= 1;
        }

        if (Players.Any(player => player.Lives == 0))
            result.IsLastRound = true;
        else
            SetupNextRound();
        return result;
    }

    public GameResult CalculateGameResult()
    {
        if (!IsGameFinished) throw new Exception("Game result unknown yet");
        return Players[0].Lives switch
        {
            0 when Players[1].Lives == 0 => new GameResult(true, null),
            0 => new GameResult(false, Players[1].Name),
            _ => new GameResult(false, Players[0].Name)
        };
    }
}
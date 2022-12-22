﻿using Models.Dtos;
using Models.FeaturesRepo;

namespace Models;

public class Player
{
    //this constructor for server
    public Player(string name, Game game, int deckId):this(name, game)
    {
        Hand = new List<Card>();
        Deck = new Deck(DecksLibrary.Decks[deckId], true);
        for(var i =0;i<10;i++)
            PullCard();
    }
    //this constructor for client
    public Player(string name, Game game, int[] hand):this(name, game)
    {
        Hand = hand.Select(CardLibrary.GetCard).ToList();
    }
    //basic constructor
    public Player(string name, Game game)
    {
        GameField = game;
        Name = name;
        Lives = 2;
        OwnField = SetupField();
    }

    public int Lives { get; set; }

    public int Power
    {
        get { return OwnField.Sum(row => row.Power); }
    }
    public bool HasPassed { get; set; }
    public Game GameField { get; set; }
    public List<Row> OwnField { get; set; }
    public string Name { get; set; }
    public List<Card> Hand { get; set; }
    public Deck Deck { get; set; }

    public MoveResult PlayCard(int positionInHand, int rowIndex, int positionInRow)
    {
        if (positionInHand >= Hand.Count || positionInHand < 0) throw new IndexOutOfRangeException("No card in hand");
        var card = Hand[positionInHand];
        if (rowIndex != (int) card.Role) throw new ArgumentException("Wrong row");
        var rowCards = OwnField[rowIndex].Cards;
        if (positionInRow > rowCards.Count || positionInRow < 0)
            throw new IndexOutOfRangeException("Wrong position in a row");
        var result = new MoveResult(this, positionInHand, rowIndex, positionInRow);
        rowCards.Insert(positionInRow, card);
        if(card.OwnImpact.TriggerType == TriggerType.OnPlay)
            card.OwnImpact.Impact(GameField, this, result);
        return result;
    }

    public MoveResult Pass()
    {
        var result = new MoveResult(this, true);
        HasPassed = true;
        return result;
    }

    public void PullCard(MoveResult result)
    {
        PullCard();
        result.PulledCards.Add(Hand[^1].Id);
    }

    public void PullCard()
    {
        if (Deck.Cards.Count == 0) return;
        var card = Deck.Cards[0];
        Hand.Add(card);
        Deck.Cards.Remove(card);
    }

    public static List<Row> SetupField() => new() {new Row(Role.Melee), new Row(Role.Shooter)};
    
}
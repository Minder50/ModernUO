using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Multis;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PlayerHouseLimitTests : IDisposable
{
    private readonly MobileTestHarness _harness = new();
    private readonly List<BaseHouse> _houses = new();

    [Fact]
    public void FirstHouseIsPrimaryAndDeletionUpdatesCount()
    {
        var owner = NewPlayer(1400);
        var house = NewHouse(owner, 1400);

        Assert.Same(house, owner.PriPropiedad);
        Assert.Equal(1, owner.NumCasas);
        Assert.False(owner.CanAddHouse());

        house.Delete();

        Assert.Null(owner.PriPropiedad);
        Assert.Equal(0, owner.NumCasas);
        Assert.True(owner.CanAddHouse());
    }

    [Fact]
    public void RevokingPrivilegeDeletesAdditionalOwnedHouses()
    {
        var owner = NewPlayer(1500);
        var primary = NewHouse(owner, 1500);

        owner.MaxCasas = 3;
        Assert.Equal(1, owner.MaxCasas);

        owner.Privilegiado = true;
        owner.MaxCasas = 2;
        var additional = NewHouse(owner, 1550);

        Assert.Equal(2, owner.NumCasas);
        Assert.True(owner.CanAddHouse(additional));
        Assert.False(owner.CanAddHouse());

        owner.Privilegiado = false;

        Assert.True(additional.Deleted);
        Assert.Same(primary, owner.PriPropiedad);
        Assert.Equal(1, owner.NumCasas);
        Assert.Equal(1, owner.MaxCasas);
    }

    [Fact]
    public void CoOwnershipConsumesAndReleasesAHouseSlot()
    {
        var owner = NewPlayer(1600);
        var coOwner = NewPlayer(1650);
        var house = NewHouse(owner, 1600);

        house.AddCoOwner(owner, coOwner);

        Assert.Equal(1, coOwner.NumCasas);
        Assert.False(coOwner.CanAddHouse());

        house.RemoveCoOwner(owner, coOwner);

        Assert.Equal(0, coOwner.NumCasas);
        Assert.True(coOwner.CanAddHouse());
        Assert.Equal(1, owner.NumCasas);
    }

    [Fact]
    public void TransferMovesHouseCountAndPrimaryReference()
    {
        var previousOwner = NewPlayer(1700);
        var newOwner = NewPlayer(1750);
        var house = NewHouse(previousOwner, 1700);

        house.Owner = newOwner;

        Assert.Equal(0, previousOwner.NumCasas);
        Assert.Null(previousOwner.PriPropiedad);
        Assert.Equal(1, newOwner.NumCasas);
        Assert.Same(house, newOwner.PriPropiedad);
    }

    public void Dispose()
    {
        foreach (var house in _houses)
        {
            house.Delete();
        }

        _harness.Dispose();
    }

    private PlayerMobile NewPlayer(int x)
    {
        var player = _harness.NewPlayer();
        player.Player = true;
        player.MoveToWorld(new Point3D(x, 1400, 0), Map.Felucca);
        return player;
    }

    private BaseHouse NewHouse(PlayerMobile owner, int x)
    {
        var house = new SmallOldHouse(owner, 0x64);
        _houses.Add(house);
        house.MoveToWorld(new Point3D(x, 1400, 0), Map.Felucca);
        return house;
    }
}

using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace Server.Tests.Mobiles;

[Collection("Sequential UOContent Tests")]
public class VendorPurchaseTests : IDisposable
{
    private readonly MobileTestHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private sealed class BandageBuyingSB : SBInfo
    {
        private readonly GenericSellInfo _sellInfo = new();

        public override IShopSellInfo SellInfo => _sellInfo;

        public override List<GenericBuyInfo> BuyInfo { get; } =
        [
            new GenericBuyInfo(typeof(Bandage), 100, 10, 0xE21, 0)
        ];
    }

    private sealed class BandageBuyingVendor : TestVendor
    {
        protected override void AddSBInfos(List<SBInfo> sbInfos) => sbInfos.Add(new BandageBuyingSB());
    }

    [Theory]
    [InlineData(120, true, 20)]
    [InlineData(30, false, 30)]
    public void Purchase_UsesBankWhenBackpackCannotCoverCost(int bankGoldAmount, bool expectedSuccess, int expectedBankGold)
    {
        var vendor = _harness.Track(new BandageBuyingVendor());
        var buyer = _harness.NewPlayer();
        var backpack = new Backpack();
        buyer.AddItem(backpack);

        var backpackGold = new Gold(40);
        backpack.DropItem(backpackGold);

        var bankGold = new Gold(bankGoldAmount);
        buyer.BankBox.DropItem(bankGold);

        vendor.UpdateBuyInfo();
        var buyInfo = Assert.IsType<GenericBuyInfo>(vendor.GetBuyInfo()[0]);
        var displayEntity = buyInfo.GetDisplayEntity();

        var result = vendor.OnBuyItems(buyer, [new BuyItemResponse(displayEntity.Serial, 1)]);

        Assert.Equal(expectedSuccess, result);
        Assert.Equal(40, backpackGold.Amount);
        Assert.Same(backpack, backpackGold.Parent);
        Assert.Equal(expectedBankGold, bankGold.Amount);

        if (expectedSuccess)
        {
            Assert.NotNull(backpack.FindItemByType<Bandage>());
        }
    }
}

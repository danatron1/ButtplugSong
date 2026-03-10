using GlobalSettings;

namespace ButtplugSong.GUI.VibeSettings.LimitSettings;

internal class MinimumBelowFullHealth() : MinimumWithScale("BelowFullHealth", false, 4, true)
{
    private static PlayerData playerData => PlayerData.instance;
    public override bool IsRelevant() => playerData != null && GetScale() > 0;
    protected override float GetScale() => playerData.maxHealth - playerData.health; //returns missing health amount
}
internal class MinimumCursed() : MinimumBase("Cursed", false, 25)
{
    public override bool IsRelevant() => Gameplay.CursedCrest.IsEquipped;
}
internal class MinimumFreezing() : MinimumWithScale("Freezing", false, 25, true)
{
    public override bool IsRelevant() => hero != null && hero.isInFrostRegion && PlayerData.HasInstance && !PlayerData.instance.hasDoubleJump;
    protected override float GetScale() => hero.frostAmount;
}
internal class MinimumMaggoted() : MinimumBase("Maggoted", true, 20)
{
    public override bool IsRelevant() => hero != null && hero.cState.isMaggoted;
}
internal class MinimumNaked() : MinimumBase("Naked", true, 10)
{
    public override bool IsRelevant() => hero != null && hero.Config.ForceBareInventory;
}
internal class MinimumSwimming() : MinimumBase("Swimming", false, 5)
{
    public override bool IsRelevant() => hero != null && hero.cState.swimming;
}
internal class MinimumTouchingGround() : MinimumBase("TouchingGround", false, 5)
{
    public override bool IsRelevant() => hero != null && hero.isGameplayScene && hero.CheckTouchingGround();
}
internal class MinimumSprinting() : MinimumBase("Sprinting", false, 5)
{
    //Dashing is sprinting, but sprinting is not dashing. It's like a fingers and thumbs thing.
    public override bool IsRelevant() => hero != null && (hero.cState.isSprinting || hero.cState.isBackSprinting || hero.cState.isBackScuttling || hero.cState.dashing);
}
internal class MinimumSittingAtBench() : MinimumBase("SittingAtBench", false, 10)
{
    public override bool IsRelevant() => PlayerData.HasInstance && PlayerData.instance.atBench;
}
internal class MinimumSoaring() : MinimumBase("Soaring", true, 10)
{
    public override bool IsRelevant() => hero != null && hero.cState.superDashing;
}
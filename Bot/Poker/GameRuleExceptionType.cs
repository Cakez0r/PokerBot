namespace Poker
{
    public enum GameRuleExceptionType
    {
        PlayerBankrupt,
        IllegalCheck,
        BetLessThanAmountToCall,
        RaiseLessThanMinRaise,
        InvalidAction
    }
}
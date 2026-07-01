namespace QryptoCard.Sec
{
    /// <summary>
    /// Single source of truth for the deposit-into-card streaming kill-switch's setting/env keys.
    /// Both tiers (INT CardFundingIntentService, INT.Callback CardFundingSettlementService) keep their
    /// own Enabled() (each needs its own tier's DB read), but reference these SAME key names so the two
    /// copies can never drift apart and leave the streaming path half-enabled (RT round 4).
    /// </summary>
    public static class CardFundingGate
    {
        public const string SettingEnabled = "CardFundingStreamingEnabled"; // tblM_Setting.Name / Value >= 1
        public const string EnvEnabled = "CARD_FUNDING_STREAMING_ENABLED";  // env override ("1"/"true")
    }
}

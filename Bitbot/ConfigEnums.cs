using Binance.Net.Enums;
using System;
using System.ComponentModel;
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

namespace Bitbot
{
    internal enum Currency
    {
        [Description("BTC")] Bitcoin,
        [Description("ETH")] Ether,
        [Description("XRP")] Xrp,
        [Description("LTC")] Litecoin,
        [Description("EOS")] Eos,
        [Description("XLM")] Stellar,
        //[Description("BCH")] BitcoinCash,
        //[Description("XTZ")] Tezos,
        //[Description("ETC")] EtherClassic,
        //[Description("OMG")] OmgNetwork,
        //[Description("LINK")] Chainlink,
        //[Description("ZRX")] Ox,
        //[Description("ALGO")] Algorand,
        //[Description("BAND")] BandProtocol,
        //[Description("NMR")] Numeraire,
        //[Description("CGLD")] Celo,
        //[Description("UMA")] Uma,
    }

    internal enum Interval
    {
        [Description("3min")] T3M = 3,
        [Description("5min")] T5M = 5,
        [Description("15min")] T15M = 15,
        [Description("30min")] T30M = 30,
        [Description("1h")] T1H = 60,
        [Description("2h")] T2H = 120,
        [Description("4h")] T4H = 240,
        [Description("6h")] T6H = 360,
        [Description("8h")] T8H = 480,
        [Description("12h")] T12H = 720,
        [Description("1d")] T1D = 1440,
    }

    internal static class ConvertEnum
    {
        internal static KlineInterval ToBinance(this Interval interval) =>
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            interval switch
            {
                Interval.T3M => KlineInterval.ThreeMinutes,
                Interval.T5M => KlineInterval.FiveMinutes,
                Interval.T15M => KlineInterval.FifteenMinutes,
                Interval.T30M => KlineInterval.ThirtyMinutes,
                Interval.T1H => KlineInterval.OneHour,
                Interval.T2H => KlineInterval.TwoHour,
                Interval.T4H => KlineInterval.FourHour,
                Interval.T6H => KlineInterval.SixHour,
                Interval.T8H => KlineInterval.EightHour,
                Interval.T12H => KlineInterval.TwelveHour,
                Interval.T1D => KlineInterval.OneDay,
                _ => throw new Exception("Invalid Interval")
            };
    }
}

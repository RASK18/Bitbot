using System.ComponentModel;
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

namespace Bitbot
{
    internal enum Environment
    {
        [Description("DEV")] Dev,
        [Description("PRO")] Pro
    }

    internal enum Interval
    {
        [Description("1min")] T1M,
        [Description("5min")] T5M,
        [Description("15min")] T15M,
        [Description("1h")] T1H,
        [Description("6h")] T6H,
        [Description("1d")] T1D
    }

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
}

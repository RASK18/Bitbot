using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.WalletData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bitbot
{
    internal class Session
    {
        private readonly BinanceClient _client;

        public string Currency { get; }
        public string Pair => $"{Currency}EUR";
        public string Interval { get; set; }
        public int IntervalMin { get; set; }
        public decimal TakerFee { get; set; }
        public decimal EurAvailable { get; set; }
        public decimal CryptoAvailable { get; set; }
        public decimal Balance { get; set; }
        public IList<string> Logs { get; set; }

        public Session(Currency currency, BinanceClient client)
        {
            _client = client;
            Logs = new List<string>();
            Currency = currency.GetDescription();

            UpdateFee();
            UpdateAvailable();
        }

        public void UpdateFee()
        {
            IEnumerable<BinanceTradeFee> fees = _client.Spot.Market.GetTradeFee(Pair).GetResult();
            TakerFee = fees.Single().TakerFee;
        }

        public void SearchInterval()
        {
            Array intervals = typeof(Interval).GetEnumValues();
            foreach (Interval interval in intervals)
            {
                bool isGrowing = CheckKline(interval);

                if (!isGrowing) continue;

                Interval = interval.GetDescription();
                IntervalMin = (int)interval;
                break;
            }
        }

        public void UpdateBalance(decimal change, string log)
        {
            Logs.Add(log);
            Balance += change;
            UpdateAvailable();
            UiController.PrintSession(this);
        }

        private void UpdateAvailable()
        {
            decimal price = _client.Spot.Market.GetPrice(Currency + "EUR").GetResult().Price;
            IEnumerable<BinanceUserCoin> coins = _client.General.GetUserCoins().GetResult().ToList();
            BinanceUserCoin euro = coins.Single(a => a.Coin == "EUR");
            BinanceUserCoin crypto = coins.Single(a => a.Coin == Currency);

            EurAvailable = euro.Free.Round();
            CryptoAvailable = (crypto.Free * price).Round();
        }

        private bool CheckKline(Interval interval)
        {
            KlineInterval kline = interval.ToBinance();
            IEnumerable<IBinanceKline> candles = _client.Spot.Market
                                                             .GetKlines(Pair, kline, null, null, 2)
                                                             .GetResult();
            IBinanceKline candle = candles.First();
            decimal? wantedHigh = candle.Open * TakerFee * 2 + candle.Open;

            return candle.Open <= candle.Close && !(wantedHigh > candle.High);
        }
    }
}

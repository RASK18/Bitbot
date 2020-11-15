using Coinbase.Pro;
using Coinbase.Pro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable StringLiteralTypo

namespace Bitbot
{
    internal class Program
    {
        private static readonly Config ConfigDev = new Config
        {
            ApiKey = "**********",
            Secret = "**********",
            Passphrase = "**********",
            ApiUrl = "https://api-public.sandbox.pro.coinbase.com" // Sandbox
        };

        private static readonly Config ConfigPro = new Config
        {
            ApiKey = "**********",
            Secret = "**********",
            Passphrase = "**********",
        };

        private static string _productId;
        private static Session _session;
        private static CoinbaseProClient _client;

        private static async Task Main()
        {
            UiController.Start();
            Environment environment = UiController.AskRadio<Environment>("Entorno", (int)Environment.Pro);
            Interval interval = UiController.AskRadio<Interval>("Intervalo", (int)Interval.T6H);
            Currency currency = UiController.AskRadio<Currency>("Producto", (int)Currency.Xrp);

            Config config = environment == Environment.Pro ? ConfigPro : ConfigDev;
            _client = new CoinbaseProClient(config);

            _session = new Session(environment, interval, currency, _client);
            _productId = $"{_session.Currency}-EUR";

            UiController.PrintSession(_session);

            while (true)
            {
                DateTime start = DateTime.Now;
                DateTime end = start.AddSeconds(_session.IntervalSec);

                bool isOk = false;
                try
                {
                    isOk = await MakeMoney(start, end);
                }
                catch (Exception ex)
                {
                    string errorMsg = await ex.GetErrorMessageAsync();
                    Console.WriteLine(errorMsg);
                }

                TimeSpan difference = end - DateTime.Now;
                int sleep = end > DateTime.Now && !isOk ? (int)difference.TotalSeconds : 10;
                Sleep(sleep);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static async Task<bool> MakeMoney(DateTime start, DateTime end)
        {
            DateTime previous = start.AddSeconds(_session.IntervalSec * -2);

            List<Candle> candles = await _client.MarketData.GetHistoricRatesAsync(_productId, previous, start, _session.IntervalSec);
            Candle previousCandle = candles.Last();

            if (previousCandle.Open > previousCandle.Close)
            {
                Console.WriteLine(" El precio está bajando, esperamos...");
                return false;
            }

            decimal? wantedHigh = previousCandle.Open * (_session.TakerFee * 2) + previousCandle.Open;
            if (wantedHigh > previousCandle.High)
            {
                Console.WriteLine(" El precio no está subiendo lo suficiente, esperamos...");
                return false;
            }

            // Ej: BTC-EUR
            // Buy:  EUR -> BTC
            // Sell: BTC -> EUR
            // UseSize: BTC
            // UseFunds: EUR
            // Min: 10 Eur or 0.001 Btc
            // Use Taker fees

            const decimal eurTest = 15;
            Order buy = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Buy, _productId, eurTest, AmountType.UseFunds);
            buy = await CheckOrder(buy);

            string logBuy = $" Comprado: {buy.SpecifiedFunds.Round()} - {buy.FillFees.Round()} = {buy.Funds.Round()} Eur";
            await _session.UpdateBalance(-buy.SpecifiedFunds, logBuy);

            int count = 1;
            while (end > DateTime.Now)
            {
                Ticker ticker = await _client.MarketData.GetTickerAsync(_productId);
                decimal actualPrice = buy.FilledSize * ticker.Bid;
                decimal actualFees = actualPrice * _session.TakerFee;
                decimal wouldGet = actualPrice - actualFees;
                Console.WriteLine($" {count}/10 - Obtendría: {actualPrice.Round()} - {actualFees.Round()} = {wouldGet.Round()} Eur");

                if (wouldGet > eurTest)
                {
                    Order sellOk = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Sell, _productId, buy.FilledSize);
                    sellOk = await CheckOrder(sellOk);
                    decimal finalOk = sellOk.ExecutedValue - sellOk.FillFees;

                    string logSellOk = $" Vendido: {sellOk.ExecutedValue.Round()} - {sellOk.FillFees.Round()} = {finalOk.Round()} Eur";
                    await _session.UpdateBalance(finalOk, logSellOk);
                    return true;
                }

                Sleep(_session.IntervalSec / 10);
                count++;
            }

            Order sellKo = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Sell, _productId, buy.FilledSize);
            sellKo = await CheckOrder(sellKo);
            decimal finalKo = sellKo.ExecutedValue - sellKo.FillFees;

            string logSellKo = $" Vendido: {sellKo.ExecutedValue.Round()} - {sellKo.FillFees.Round()} = {finalKo.Round()} Eur";
            await _session.UpdateBalance(finalKo, logSellKo);
            return false;
        }

        private static async Task<Order> CheckOrder(Order order)
        {
            while (order.Status != "done")
            {
                order = await _client.Orders.GetOrderAsync(order.Id);
                Thread.Sleep(500);
            }

            return order;
        }

        private static void Sleep(int seconds)
        {
            for (int i = seconds; i >= 0; i--)
            {
                Console.Write($"\r Esperando {i} seg...");
                Thread.Sleep(1000);
            }
            Console.Write("\r                    ");
            Console.Write("\r");
        }

    }
}

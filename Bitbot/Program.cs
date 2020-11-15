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

        private static Environment _environmentEnum;
        private static Interval _intervalEnum;
        private static Currency _currencyEnum;
        private static decimal _takerFee;

        private static int _interval;
        private static string _currency;
        private static string _productId;
        private static CoinbaseProClient _client;
        private static decimal _balance;

        private static async Task Main()
        {
            Console.Title = "Bitbot";
            UiController.PrintLine();
            _environmentEnum = UiController.AskRadio<Environment>("Entorno", (int)Environment.Pro);
            _intervalEnum = UiController.AskRadio<Interval>("Intervalo", (int)Interval.T6H);
            _currencyEnum = UiController.AskRadio<Currency>("Producto", (int)Currency.Xrp);

            await SetConfig();
            await UpdateAccounts();

            while (true)
            {
                DateTime start = DateTime.Now;
                DateTime end = start.AddSeconds(_interval);

                try
                {
                    await MakeMoney(start, end);
                }
                catch (Exception ex)
                {
                    string errorMsg = await ex.GetErrorMessageAsync();
                    Console.WriteLine(errorMsg);
                }

                TimeSpan difference = end - DateTime.Now;
                int sleep = end > DateTime.Now ? (int)difference.TotalSeconds : 10;
                Sleep(sleep);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static async Task SetConfig()
        {
            _interval = _intervalEnum switch
            {
                Interval.T1M => 60,
                Interval.T5M => 300,
                Interval.T15M => 900,
                Interval.T1H => 3600,
                Interval.T6H => 21600,
                Interval.T1D => 86400,
                _ => 21600
            };

            _currency = _currencyEnum.GetDescription();
            _productId = $"{_currency}-EUR";

            Config config = _environmentEnum == Environment.Dev ? ConfigDev : ConfigPro;
            _client = new CoinbaseProClient(config);

            MakerTakerFees fees = await _client.Fees.GetCurrentFeesAsync();
            _takerFee = fees.TakerFeeRate;
        }

        private static async Task UpdateAccounts()
        {
            Console.Clear();
            UiController.PrintLine();
            Console.WriteLine($" Entorno: {_environmentEnum.GetDescription()}");
            Console.WriteLine($" Intervalo: {_intervalEnum.GetDescription()}");
            Console.WriteLine($" Producto: {_currencyEnum.GetDescription()}");
            Console.WriteLine($" Tarifa: {Math.Round(_takerFee * 100, 1)}%");
            UiController.PrintLine();

            List<Account> accounts = await _client.Accounts.GetAllAccountsAsync();
            Account euro = accounts.Single(a => a.Currency == "EUR");
            Console.WriteLine($" EUR: {euro.Available.Round()} Eur");

            Account crypto = accounts.Single(a => a.Currency == _currency);
            Ticker ticker = await _client.MarketData.GetTickerAsync(_productId);
            decimal equivalent = (crypto.Available * ticker.Price).Round();
            Console.WriteLine($" {_currency}: {equivalent} Eur");

            Console.ForegroundColor = _balance >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($" Balance de sesión: {_balance.Round()} Eur");
            Console.ForegroundColor = ConsoleColor.White;

            UiController.PrintLine();
        }

        private static async Task MakeMoney(DateTime start, DateTime end)
        {
            DateTime previous = start.AddSeconds(_interval * -2);

            List<Candle> candles = await _client.MarketData.GetHistoricRatesAsync(_productId, previous, start, _interval);
            Candle previousCandle = candles.Last();

            if (previousCandle.Open > previousCandle.Close)
            {
                Console.WriteLine(" El precio está bajando, esperamos...");
                return;
            }

            decimal? wantedHigh = previousCandle.Open * (_takerFee * 2) + previousCandle.Open;
            if (wantedHigh > previousCandle.High)
            {
                Console.WriteLine(" El precio no está subiendo lo suficiente, esperamos...");
                return;
            }

            // Ej: BTC-EUR
            // Buy:  EUR -> BTC
            // Sell: BTC -> EUR
            // UseSize: BTC
            // UseFunds: EUR
            // Min: 10 Eur or 0.001 Btc
            // Use Taker fees

            const decimal eurTest = 15;
            Console.WriteLine($" Comprando {_currency}...");
            Order buy = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Buy, _productId, eurTest, AmountType.UseFunds);
            buy = await CheckOrder(buy);
            _balance -= buy.SpecifiedFunds;

            await UpdateAccounts();
            Console.WriteLine($" Comprado: {buy.SpecifiedFunds.Round()} - {buy.FillFees.Round()} = {buy.Funds.Round()} Eur");

            int count = 1;
            while (end > DateTime.Now)
            {
                Ticker ticker = await _client.MarketData.GetTickerAsync(_productId);
                decimal actualPrice = buy.FilledSize * ticker.Bid;
                decimal actualFees = actualPrice * _takerFee;
                decimal wouldGet = actualPrice - actualFees;
                Console.WriteLine($" {count}/10 - Obtendría: {actualPrice.Round()} - {actualFees.Round()} = {wouldGet.Round()} Eur");

                if (wouldGet > eurTest)
                {
                    Console.WriteLine($" Vendiendo {_currency}...");
                    Order sellOk = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Sell, _productId, buy.FilledSize);
                    sellOk = await CheckOrder(sellOk);
                    _balance += sellOk.ExecutedValue;

                    await UpdateAccounts();
                    Console.WriteLine($" Vendido: {sellOk.SpecifiedFunds.Round()} - {sellOk.FillFees.Round()} = {sellOk.Funds.Round()} Eur");
                    return;
                }

                Sleep(_interval / 10);
                count++;
            }

            Console.WriteLine(" Game Over");
            Console.WriteLine($" Vendiendo {_currency}...");
            Order sellKo = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Sell, _productId, buy.FilledSize);
            sellKo = await CheckOrder(sellKo);
            decimal final = sellKo.ExecutedValue - sellKo.FillFees;
            _balance += final;

            await UpdateAccounts();
            Console.WriteLine($" Vendido: {sellKo.ExecutedValue.Round()} - {sellKo.FillFees.Round()} = {final.Round()} Eur");
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

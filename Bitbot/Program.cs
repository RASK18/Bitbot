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

        private static int _intervals;
        private static MakerTakerFees _fees;
        private static CoinbaseProClient _client;
        private static string _currency;
        private static string _productId;

        private static async Task Main()
        {
            Console.Title = "Bitbot";
            Console.WriteLine();

            AskConfigs();

            _fees = await _client.Fees.GetCurrentFeesAsync();
            Console.WriteLine($" Tarifa: {Math.Round(_fees.TakerFeeRate * 100, 2)}%");

            while (true)
            {
                DateTime start = DateTime.Now;
                DateTime end = start.AddSeconds(_intervals);

                Console.WriteLine();
                Console.WriteLine("-------------------------");

                try
                {
                    List<Account> accounts = await _client.Accounts.GetAllAccountsAsync();
                    Account euro = accounts.Single(a => a.Currency == "EUR");
                    Console.WriteLine($" EUR: {Math.Round(euro.Available, 2)} Eur");

                    Account crypto = accounts.Single(a => a.Currency == _currency);
                    Ticker ticker = await _client.MarketData.GetTickerAsync(_productId);
                    decimal equivalent = Math.Round(crypto.Available * ticker.Price, 2);
                    Console.WriteLine($" {_currency}: {equivalent} Eur");
                    Console.WriteLine();

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

        private static void AskConfigs()
        {
            Environment env = UiController<Environment>.AskRadio("Entorno", (int)Environment.Pro);
            Config config = env == Environment.Dev ? ConfigDev : ConfigPro;
            _client = new CoinbaseProClient(config);

            Intervals intervals = UiController<Intervals>.AskRadio("Intervalo", (int)Intervals.T6H);
            _intervals = intervals switch
            {
                Intervals.T1M => 60,
                Intervals.T5M => 300,
                Intervals.T15M => 900,
                Intervals.T1H => 3600,
                Intervals.T6H => 21600,
                Intervals.T1D => 86400,
                _ => 21600
            };

            Currencies currency = UiController<Currencies>.AskRadio("Producto", (int)Currencies.Xrp);
            _currency = currency.GetDescription();
            _productId = $"{_currency}-EUR";
        }

        private static async Task MakeMoney(DateTime start, DateTime end)
        {
            DateTime previous = start.AddSeconds(_intervals * -2);

            List<Candle> candles = await _client.MarketData.GetHistoricRatesAsync(_productId, previous, start, _intervals);
            Candle previousCandle = candles.Last();

            if (previousCandle.Open > previousCandle.Close)
            {
                Console.WriteLine(" El precio está bajando, esperamos...");
                return;
            }

            decimal? wantedClose = previousCandle.Open * 0.01m + previousCandle.Open;
            if (wantedClose > previousCandle.High)
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

            const decimal pruebas = 15;
            Console.WriteLine($" Comprando {pruebas} Eur en {_currency}...");
            Order buy = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Buy, _productId, pruebas, AmountType.UseFunds);
            buy = await CheckOrder(buy);
            Console.WriteLine($" ¡Comprando! - Impuestos compra: {Math.Round(buy.FillFees, 2)} Eur");

            int count = 1;
            while (end > DateTime.Now)
            {
                Ticker ticker = await _client.MarketData.GetTickerAsync(_productId);
                decimal aux1 = buy.FilledSize * ticker.Price;
                decimal canGet = aux1 - aux1 * _fees.TakerFeeRate;
                decimal canGetRound = Math.Round(canGet, 2);
                Console.WriteLine($" Intento {count} - Precio: {canGetRound} Eur");

                if (canGet > pruebas)
                {
                    Console.WriteLine($" Vendiendo {canGetRound} Eur en {_currency}...");
                    Order sell1 = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Sell, _productId, buy.FilledSize);
                    sell1 = await CheckOrder(sell1);
                    Console.WriteLine($" ¡Vendido! - Impuestos venta: {Math.Round(sell1.FillFees, 2)} Eur");
                    Console.WriteLine($" Beneficio: {sell1.ExecutedValue - pruebas} Eur");
                    return;
                }

                Sleep(_intervals / 10);
                count++;
            }

            Console.WriteLine($" Game Over, vendiendo {_currency}...");
            Order sell2 = await _client.Orders.PlaceMarketOrderAsync(OrderSide.Sell, _productId, buy.FilledSize);
            sell2 = await CheckOrder(sell2);
            Console.WriteLine($" ¡Vendido! - Impuestos venta: {Math.Round(sell2.FillFees, 2)} Eur");
            Console.WriteLine($" Perdidas: {Math.Round(pruebas - sell2.ExecutedValue, 2)} Eur");
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

using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Authentication;
using System;
using System.Linq;
using System.Threading;

// ReSharper disable StringLiteralTypo

namespace Bitbot
{
    internal class Program
    {
        private const string Key = "***********";
        private const string Secret = "***********";
        private static readonly BinanceClient Client = new BinanceClient(new BinanceClientOptions
        {
            ApiCredentials = new ApiCredentials(Key, Secret)
        });

        private static Session _session;

        private static void Main()
        {
            UiController.Start();
            Currency currency = UiController.AskRadio<Currency>("Moneda");
            decimal profit = UiController.AskNumber("Ganancias (%/d)", 0.75m);

            _session = new Session(currency, profit, Client);

            while (true)
            {
                _session.SearchInterval();
                UiController.PrintSession(_session);

                try
                {
                    MakeMoney();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.ReadKey();
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        // Ej: BTC-EUR
        // Buy:  EUR -> BTC
        // Sell: BTC -> EUR
        private static void MakeMoney()
        {
            BinancePlacedOrder buyPlaced = Client.Spot.Order
                                                .PlaceOrder(_session.Pair, OrderSide.Buy, OrderType.Market, null, _session.EurAvailable)
                                                .GetResult();

            decimal buyFee = GetFee(buyPlaced);
            BinanceOrder buy = WaitOrder(buyPlaced);

            _session.WantedPrice = buy.QuoteQuantity + buy.QuoteQuantity * _session.Profit / 100;
            string logBuy = $" Comprado: {buy.QuoteQuantity.Round()} - {buyFee.Round()} = {buy.QuoteQuantityFilled.Round()} Eur";
            _session.Logs.Add(logBuy);
            _session.UpdateAvailable();
            UiController.PrintSession(_session);

            int count = 1;
            bool exit = false;
            while (!exit)
            {
                Sleep(_session.IntervalMin * 6); // 10 veces

                decimal price = Client.Spot.Market.GetPrice(_session.Pair).GetResult().Price;
                decimal actualPrice = buy.QuantityFilled * price; // Antes o despues de impuestos?
                decimal actualFees = actualPrice * _session.TakerFee;
                decimal wouldGet = actualPrice - actualFees;

                Console.WriteLine($" {count}/10 - Obtendría: {actualPrice.Round()} - {actualFees.Round()} = {wouldGet.Round()} Eur");

                if (wouldGet >= _session.WantedPrice)
                    exit = true;

                if (count >= 10) break;

                count++;
            }

            BinancePlacedOrder sellPlaced = Client.Spot.Order
                                                 .PlaceOrder(_session.Pair, OrderSide.Sell, OrderType.Market, buy.QuantityFilled)
                                                 .GetResult();

            decimal sellFee = GetFee(buyPlaced);
            BinanceOrder sell = WaitOrder(sellPlaced);

            decimal finalSell = sell.QuoteQuantityFilled - sellFee;
            string logSell = $" Vendido: {sell.QuoteQuantityFilled.Round()} - {sellFee.Round()} = {finalSell.Round()} Eur";
            _session.Logs.Add(logSell);

            decimal balance = finalSell - buy.QuoteQuantity;
            _session.UpdateBalance(balance);
        }

        private static decimal GetFee(BinancePlacedOrder placed)
        {
            if (placed.Fills == null)
                throw new Exception("No hay fills! (Ver Account Trade List)");

            decimal feePrice = Client.Spot.Market.GetPrice("BNBEUR").GetResult().Price;
            decimal fee = placed.Fills.Sum(f => f.Commission) * feePrice;
            return fee;
        }

        private static BinanceOrder WaitOrder(BinancePlacedOrder placedOrder)
        {
            BinanceOrder order = Client.Spot.Order.GetOrder(_session.Pair, placedOrder.OrderId, placedOrder.ClientOrderId).GetResult();

            while (order.Status != OrderStatus.Filled)
            {
                Thread.Sleep(1000);
                order = Client.Spot.Order.GetOrder(_session.Pair, placedOrder.OrderId, placedOrder.ClientOrderId).GetResult();
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

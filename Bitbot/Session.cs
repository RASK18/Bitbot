using Coinbase.Pro;
using Coinbase.Pro.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bitbot
{
    internal class Session
    {
        private readonly CoinbaseProClient _client;

        public int IntervalSec { get; }
        public string Interval { get; }
        public string Environment { get; }
        public string Currency { get; }
        public decimal TakerFee { get; set; }
        public decimal EurAvailable { get; set; }
        public decimal CryptoAvailable { get; set; }
        public decimal Balance { get; set; }
        public IList<string> Logs { get; set; }

        public Session(Environment environment, Interval interval, Currency currency, CoinbaseProClient client)
        {
            Logs = new List<string>();
            Currency = currency.GetDescription();
            Environment = environment.GetDescription();
            Interval = interval.GetDescription();
            IntervalSec = interval switch
            {
                Bitbot.Interval.T1M => 60,
                Bitbot.Interval.T5M => 300,
                Bitbot.Interval.T15M => 900,
                Bitbot.Interval.T1H => 3600,
                Bitbot.Interval.T6H => 21600,
                Bitbot.Interval.T1D => 86400,
                _ => 21600
            };

            _client = client;
            UpdateFee().GetAwaiter().GetResult();
            UpdateAvailable().GetAwaiter().GetResult();
        }

        public async Task UpdateFee()
        {
            MakerTakerFees fees = await _client.Fees.GetCurrentFeesAsync();
            TakerFee = fees.TakerFeeRate;
        }

        public async Task UpdateBalance(decimal change, string log)
        {
            Logs.Add(log);
            Balance += change;
            await UpdateAvailable();
            UiController.PrintSession(this);
        }

        private async Task UpdateAvailable()
        {
            List<Account> accounts = await _client.Accounts.GetAllAccountsAsync();
            Account euro = accounts.Single(a => a.Currency == "EUR");
            EurAvailable = euro.Available.Round();

            Account crypto = accounts.Single(a => a.Currency == Currency);
            Ticker ticker = await _client.MarketData.GetTickerAsync(Currency + "-EUR");
            CryptoAvailable = (crypto.Available * ticker.Ask).Round();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Bitbot
{
    internal static class UiController
    {
        public static void Start()
        {
            Console.Title = "Bitbot";
            Console.SetWindowSize(80, 40);
            PrintLine();
        }

        public static void BeepMario()
        {
            Console.Beep(659, 125);
            Console.Beep(659, 125);
            Thread.Sleep(125);
            Console.Beep(659, 125);
            Thread.Sleep(167);
            Console.Beep(523, 125);
            Console.Beep(659, 125);
            Thread.Sleep(125);
            Console.Beep(784, 125);
        }

        public static void BeepWrong()
        {
            Console.Beep(523, 125);
            Console.Beep(392, 125);
        }

        private static void PrintLine() => Console.WriteLine(new string('-', 60));

        public static void PrintSession(Session session)
        {
            Console.Clear();
            PrintLine();
            Console.WriteLine($" Moneda: {session.Currency}");
            Console.WriteLine($" Tarifa: {(session.TakerFee * 100).Round()}%");
            Console.WriteLine($" Intervalo: {session.Interval}");
            PrintLine();
            Console.WriteLine($" EUR: {session.EurAvailable} Eur");
            Console.WriteLine($" {session.Currency}: {session.CryptoAvailable} Eur");
            Console.ForegroundColor = session.Balance >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($" Balance de sesión: {session.Balance.Round()} Eur");
            Console.ForegroundColor = ConsoleColor.White;
            PrintLine();
            Console.WriteLine($" Precio deseado: {session.WantedPrice.Round()} Eur");
            PrintLine();

            foreach (string log in session.Logs)
                Console.WriteLine(log);
        }

        public static decimal AskNumber(string label, decimal? def = null)
        {
            decimal result = 0;

            while (result == 0)
            {
                Console.Write($" {label}: ");
                string input = Console.ReadLine()?.Replace('.', ',');

                if (input == "") input = def?.ToString();
                if (input != "0" && decimal.TryParse(input, out result)) continue;

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write("\r" + new string(' ', 60));
                Console.Write("\r");
            }

            return result;
        }

        public static T AskRadio<T>(string label, int init = 0) where T : struct, Enum
        {
            bool exit = false;
            int option = init;
            int maxOptions = typeof(T).GetEnumValues().Length - 1;

            while (!exit)
            {
                PrintRadio<T>(label, ref option);
                exit = ReadKey(ref option, maxOptions);
            }

            Console.Write($"\r {label}: {GetDescription<T>(option)}");
            Console.WriteLine(new string(' ', 40));

            return (T)(object)option;
        }

        private static void PrintRadio<T>(string label, ref int option) where T : struct, Enum
        {
            Console.Write($"\r {label}: ");

            foreach (FieldInfo field in GetFields<T>())
            {
                bool isOption = option == GetValue(field);

                if (isOption)
                    Console.ForegroundColor = ConsoleColor.Green;

                Console.Write(isOption ? ">" : "·");
                Console.Write($" {field.GetDescription()}  ");

                if (isOption)
                    Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private static bool ReadKey(ref int option, int maxOptions)
        {
            ConsoleKey key = Console.ReadKey().Key;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (key)
            {
                case ConsoleKey.LeftArrow when option == 0:
                    option = maxOptions;
                    break;
                case ConsoleKey.LeftArrow when option > 0:
                    option--;
                    break;
                case ConsoleKey.RightArrow when option < maxOptions:
                    option++;
                    break;
                case ConsoleKey.RightArrow when option == maxOptions:
                    option = 0;
                    break;
                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    return true;
            }

            return false;
        }

        private static IEnumerable<FieldInfo> GetFields<T>() where T : struct, Enum => typeof(T).GetFields().Skip(1);
        private static int GetValue(FieldInfo field) => (int)(field.GetValue(null) ?? throw new Exception());
        private static string GetDescription<T>(int value) where T : struct, Enum =>
            GetFields<T>().SingleOrDefault(f => value == GetValue(f)).GetDescription();
    }
}

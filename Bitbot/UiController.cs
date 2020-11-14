using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bitbot
{
    internal static class UiController<T> where T : struct, Enum
    {
        private static readonly int MaxOptions = typeof(T).GetEnumValues().Length - 1;

        public static T AskRadio(string label, int init = 0)
        {
            bool exit = false;
            int option = init;

            while (!exit)
            {
                PrintRadio(label, ref option);
                exit = ReadKey(ref option);
            }

            Console.Write($"\r {label}: {GetDescription(option)}");
            Console.WriteLine(new string(' ', 40));

            return (T)(object)option;
        }

        private static void PrintRadio(string label, ref int option)
        {
            string result = $"\r {label}: ";

            foreach (FieldInfo field in GetFields())
            {
                result += option == GetValue(field) ? ">" : "·";
                result += $" {field.GetDescription()}  ";
            }

            Console.Write(result);
        }

        private static bool ReadKey(ref int option)
        {
            ConsoleKey key = Console.ReadKey().Key;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (key)
            {
                case ConsoleKey.LeftArrow when option == 0:
                    option = MaxOptions;
                    break;
                case ConsoleKey.LeftArrow when option > 0:
                    option--;
                    break;
                case ConsoleKey.RightArrow when option < MaxOptions:
                    option++;
                    break;
                case ConsoleKey.RightArrow when option == MaxOptions:
                    option = 0;
                    break;
                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    return true;
            }

            return false;
        }

        private static IEnumerable<FieldInfo> GetFields() => typeof(T).GetFields().Skip(1);
        private static int GetValue(FieldInfo field) => (int)(field.GetValue(null) ?? throw new Exception());
        private static string GetDescription(int value) => GetFields().SingleOrDefault(f => value == GetValue(f)).GetDescription();
    }
}

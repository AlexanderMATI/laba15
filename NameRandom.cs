using System;

namespace laba15
{
    class NameRandom
    {
      
        static string[] Surnames = { "Адиянов", "Воробьев", "Горшков", "Дегтярев", "Демьяненко", "Зулпукааров", "Козловский", "Комаров", "Слепцов", "Филин","Фукс", "Якупова" };

        static Random Random = new Random();

        public static string NameRand()
        {
            
            int Surname = Random.Next(0, Surnames.Length - 1);

            return $"{Surnames[Surname]}, id: {Random.Next(100,500)}";
        }
    }
}

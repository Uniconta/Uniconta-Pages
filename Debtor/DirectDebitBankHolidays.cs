using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.Common;

namespace UnicontaDirectDebitPayment
{
    public static class DirectDebitBankHolidays
    {

        public static DateTime AdjustBankDay(CountryCode country, DateTime value, bool nextBankDay = true)
        {
            var idx = nextBankDay ? 1 : -1;
            var candidate = value;
            while (!(IsBankDay(country, candidate)))
                candidate = candidate.AddDays(idx);
            return candidate;
        }

        public static bool IsBankDay(CountryCode country, DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday)
                return false;
            if (date.DayOfWeek == DayOfWeek.Sunday)
                return false;


            switch (country)
            {
                case CountryCode.Denmark:
                    if (HolidaysDK.Contains(date.Date))
                        return false;
                    break;

                case CountryCode.Netherlands:
                    if (HolidaysNL.Contains(date.Date))
                        return false;
                    break;

                default:
                    return true;
            }
            
            return true;
        }



        #region Danish holidays
        private static DateTime[] HolidaysDK =
        {
            new DateTime(2018, 01, 01), //Nytårsdag   New Year's Day 		
            new DateTime(2018, 03, 29), //Skærtorsdag     Maundy Thursday
            new DateTime(2018, 03, 30), //Langfredag  Good Friday
            new DateTime(2018, 04, 01), //Påskedag    Easter Sunday
            new DateTime(2018, 04, 02), //2. Påskedag     Easter Monday
            new DateTime(2018, 04, 27), //Store bededag   General Prayer Day
            new DateTime(2018, 05, 10), //Kristi Himmelfartsdag   Ascension Day
            new DateTime(2018, 05, 20), //Pinsedag    Pentecost
            new DateTime(2018, 05, 21), //2. Pinsedag     Whit Monday
            new DateTime(2018, 12, 25), //Juledag / 1. juledag    Christmas Day
            new DateTime(2018, 12, 26), //2. juledag  St. Stephen's Day

            new DateTime(2019, 01, 01), //Nytårsdag   New Year's Day 		
            new DateTime(2019, 04, 18), //Skærtorsdag     Maundy Thursday
            new DateTime(2019, 04, 19), //Langfredag  Good Friday
            new DateTime(2019, 04, 21), //Påskedag    Easter Sunday
            new DateTime(2019, 04, 22), //2. Påskedag     Easter Monday
            new DateTime(2019, 05, 17), //Store bededag   General Prayer Day
            new DateTime(2019, 05, 30), //Kristi Himmelfartsdag   Ascension Day
            new DateTime(2019, 06, 09), //Pinsedag    Pentecost
            new DateTime(2019, 06, 10), //2. Pinsedag     Whit Monday
            new DateTime(2019, 12, 25), //Juledag / 1. juledag    Christmas Day
            new DateTime(2019, 12, 26), //2. juledag  St. Stephen's Day

            new DateTime(2020, 01, 01), //Nytårsdag   New Year's Day 		
            new DateTime(2020, 04, 09), //Skærtorsdag     Maundy Thursday
            new DateTime(2020, 04, 10), //Langfredag  Good Friday
            new DateTime(2020, 04, 12), //Påskedag    Easter Sunday
            new DateTime(2020, 04, 13), //2. Påskedag     Easter Monday
            new DateTime(2020, 05, 08), //Store bededag   General Prayer Day
            new DateTime(2020, 05, 21), //Kristi Himmelfartsdag   Ascension Day
            new DateTime(2020, 05, 31), //Pinsedag    Pentecost
            new DateTime(2020, 06, 01), //2. Pinsedag     Whit Monday
            new DateTime(2020, 12, 25), //Juledag / 1. juledag    Christmas Day
            new DateTime(2020, 12, 26), //2. juledag  St. Stephen's Day        

            new DateTime(2021, 01, 01), //Nytårsdag   New Year's Day 		
            new DateTime(2021, 04, 01), //Skærtorsdag     Maundy Thursday
            new DateTime(2021, 04, 02), //Langfredag  Good Friday
            new DateTime(2021, 04, 04), //Påskedag    Easter Sunday
            new DateTime(2021, 04, 05), //2. Påskedag     Easter Monday
            new DateTime(2021, 04, 30), //Store bededag   General Prayer Day
            new DateTime(2021, 05, 13), //Kristi Himmelfartsdag   Ascension Day
            new DateTime(2021, 05, 23), //Pinsedag    Pentecost
            new DateTime(2021, 05, 24), //2. Pinsedag     Whit Monday
            new DateTime(2021, 12, 25), //Juledag / 1. juledag    Christmas Day
            new DateTime(2021, 12, 26), //2. juledag  St. Stephen's Da 

            new DateTime(2022, 01, 01), //Nytårsdag   New Year's Day 		
            new DateTime(2022, 04, 14), //Skærtorsdag     Maundy Thursday
            new DateTime(2022, 04, 15), //Langfredag  Good Friday
            new DateTime(2022, 04, 17), //Påskedag    Easter Sunday
            new DateTime(2022, 04, 18), //2. Påskedag     Easter Monday
            new DateTime(2022, 05, 13), //Store bededag   General Prayer Day
            new DateTime(2022, 05, 26), //Kristi Himmelfartsdag   Ascension Day
            new DateTime(2022, 06, 05), //Pinsedag    Pentecost
            new DateTime(2022, 06, 06), //2. Pinsedag     Whit Monday
            new DateTime(2022, 12, 25), //Juledag / 1. juledag    Christmas Day
            new DateTime(2022, 12, 26), //2. juledag  St. Stephen's Day        
        };
        #endregion

        #region Dutch holidays
        private static DateTime[] HolidaysNL =
        {
            new DateTime(2018, 01, 01), //Nieuwjaarsdag   New Year's Day 		 
            new DateTime(2018, 03, 30), //Goede Vrijdag   Good Friday 
            new DateTime(2018, 04, 01), //Paasfeest   Easter Sunday 
            new DateTime(2018, 04, 02), //Pasen   Easter Monday 
            new DateTime(2018, 04, 27), //Koningsdag  King's Day 		 
            new DateTime(2018, 05, 05), //Bevrijdingsdag  Liberation Day 
            new DateTime(2018, 05, 10), //Hemelvaartsdag  Ascension Day 
            new DateTime(2018, 05, 21), //Pinksteren  Whit Monday 
            new DateTime(2018, 12, 25), //Eerste kerstdag     Christmas Day 
            new DateTime(2018, 12, 26), //Tweede kerstdag     St. Stephen's Day 

            new DateTime(2019, 01, 01), // Nieuwjaarsdag   New Year's Day 		
            new DateTime(2019, 04, 19), // Goede Vrijdag   Good Friday
            new DateTime(2019, 04, 21), // Paasfeest   Easter Sunday
            new DateTime(2019, 04, 22), // Pasen   Easter Monday
            new DateTime(2019, 04, 27), // Koningsdag  King's Day 		
            new DateTime(2019, 05, 05), // Bevrijdingsdag  Liberation Day
            new DateTime(2019, 05, 30), // Hemelvaartsdag  Ascension Day
            new DateTime(2019, 06, 10), // Pinksteren  Whit Monday
            new DateTime(2019, 12, 25), // Eerste kerstdag     Christmas Day
            new DateTime(2019, 12, 26), // Tweede kerstdag     St. Stephen's Day

            new DateTime(2020, 01, 01), // Nieuwjaarsdag   New Year's Day 		
            new DateTime(2020, 04, 10), // Goede Vrijdag   Good Friday
            new DateTime(2020, 04, 12), // Paasfeest   Easter Sunday
            new DateTime(2020, 04, 13), // Pasen   Easter Monday
            new DateTime(2020, 04, 27), // Koningsdag  King's Day 		
            new DateTime(2020, 05, 05), // Bevrijdingsdag  Liberation Day
            new DateTime(2020, 05, 21), // Hemelvaartsdag  Ascension Day
            new DateTime(2020, 06, 01), // Pinksteren  Whit Monday
            new DateTime(2020, 12, 25), // Eerste kerstdag     Christmas Day
            new DateTime(2020, 12, 26), // Tweede kerstdag     St. Stephen's Day

            new DateTime(2021, 01, 01), // Nieuwjaarsdag   New Year's Day 		
            new DateTime(2021, 04, 02), // Goede Vrijdag   Good Friday
            new DateTime(2021, 04, 04), // Paasfeest   Easter Sunday
            new DateTime(2021, 04, 05), // Pasen   Easter Monday
            new DateTime(2021, 04, 27), // Koningsdag  King's Day 		
            new DateTime(2021, 05, 05), // Bevrijdingsdag  Liberation Day
            new DateTime(2021, 05, 13), // Hemelvaartsdag  Ascension Day
            new DateTime(2021, 05, 24), // Pinksteren  Whit Monday
            new DateTime(2021, 12, 25), // Eerste kerstdag     Christmas Day
            new DateTime(2021, 12, 26), // Tweede kerstdag     St. Stephen's Day

            new DateTime(2022, 01, 01), // Nieuwjaarsdag   New Year's Day 		
            new DateTime(2022, 04, 15), // Goede Vrijdag   Good Friday
            new DateTime(2022, 04, 17), // Paasfeest   Easter Sunday
            new DateTime(2022, 04, 18), // Pasen   Easter Monday
            new DateTime(2022, 04, 27), // Koningsdag  King's Day 		
            new DateTime(2022, 05, 05), // Bevrijdingsdag  Liberation Day
            new DateTime(2022, 05, 26), // Hemelvaartsdag  Ascension Day
            new DateTime(2022, 06, 06), // Pinksteren  Whit Monday
            new DateTime(2022, 12, 25), // Eerste kerstdag     Christmas Day
            new DateTime(2022, 12, 26), // Tweede kerstdag     St. Stephen's Day
        };
        #endregion
    }
}

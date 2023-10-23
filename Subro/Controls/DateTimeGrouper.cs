using System;
using System.Globalization;

namespace Subro.Controls
{
    public class DateTimeGrouper : GroupWrapper
    {
        public DateTimeGrouper(GroupingInfo grouper) : this(grouper, DateTimeGrouping.Date)
        {
        }

        public DateTimeGrouper(GroupingInfo grouper, DateTimeGrouping mode) : base(grouper)
        {
            Mode = mode;
        }

        private bool set(DateTimeGrouping val)
        {
            return (Mode & val) > (DateTimeGrouping)0;
        }

        public override Type GroupValueType
        {
            get
            {
                return
                    Mode == DateTimeGrouping.Date
                    ? typeof(DateTime)
                    : typeof(int);
            }
        }

        protected override object GetValue(object GroupValue)
        {
            DateTime dateTime = (DateTime)GroupValue;
            object result;
            if (Mode == DateTimeGrouping.Date)
                result = dateTime.Date;
            else if (Mode == DateTimeGrouping.WeekDay)
                result = (int)dateTime.DayOfWeek;
            else
            {
                int num = 0;
                if (set(DateTimeGrouping.Year))
                    num += dateTime.Year * 10000;
                if (set(DateTimeGrouping.Month))
                    num += dateTime.Month * 100;
                if (set(DateTimeGrouping.Day))
                    num += dateTime.Day;
                result = num;
            }
            return result;
        }

        public override void SetDisplayValues(GroupDisplayEventArgs e)
        {
            base.SetDisplayValues(e);
            if (Mode == DateTimeGrouping.Date)
            {
                e.DisplayValue = ((DateTime)e.Value).ToShortDateString();
            }
            else if (e.Value is int)
            {
                int num = (int)e.Value;
                string text = null;
                if (set(DateTimeGrouping.Year))
                    text = "Year: " + num / 10000;
                if (set(DateTimeGrouping.Month))
                {
                    if (text != null)
                        text += ", ";
                    text += DateTimeFormatInfo.CurrentInfo.GetMonthName(num / 100 % 100);
                }
                if (set(DateTimeGrouping.Day) || set(DateTimeGrouping.WeekDay))
                {
                    if (text != null)
                        text += ", ";
                    int dow = num % 10000;
                    text += (set(DateTimeGrouping.WeekDay) ? DateTimeFormatInfo.CurrentInfo.GetDayName((DayOfWeek)dow) : ("Day: " + dow));
                }
                e.DisplayValue = text;
            }
        }

        public readonly DateTimeGrouping Mode;
    }
}

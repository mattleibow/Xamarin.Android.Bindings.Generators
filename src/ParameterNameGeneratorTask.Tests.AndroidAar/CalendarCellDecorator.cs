using System;
using Java.Util;

namespace ParameterNameGeneratorTask.Tests.AndroidAar
{
    public abstract class CalendarCellDecorator : Java.Lang.Object, ICalendarCellDecorator
    {
        void ICalendarCellDecorator.Decorate(CalendarCellView cellView, Date date)
        {
            Decorate(cellView, CalendarPickerView.GetDate(date));
        }

        public abstract void Decorate(CalendarCellView cellView, DateTime date);
    }
}

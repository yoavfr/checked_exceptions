namespace CheckedExceptions
{
    public class ExceptionFilters
    {
        private static readonly ExceptionFilters s_Instance = new ExceptionFilters();
        public static ExceptionFilters Instance
        {
            get
            {
                return s_Instance;
            }
        }
        private ExceptionFilters() { }

        public bool FlagArgumentExceptions { get; set; }
        public bool FlagFormatExceptions { get; set; }
        public bool FlagOverflowExceptions { get; set; }
        public bool FlagAssertFailedExceptions { get; set; }
        public bool FlagNotSupportedExceptions { get; set; }
        public bool FlagNotImplementedExceptions { get; set; }
    }
}

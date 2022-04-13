using CheckedExceptions;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace CheckedExceptionsOptions
{
    class ExceptionTypeOptionsPage : DialogPage
    {
        private bool m_FormatExceptions;
        private bool m_ArgumentExceptions;
        private bool m_OverflowExceptions;
        private bool m_AssertFailedExceptions;
        private bool m_NotSupportedExceptions;
        private bool m_NotImplementedExceptions;

        [Category("Optional exceptions to track")]
        [DisplayName("System.FormatException")]
        [Description("FormatException and derived exception classes")]

        public bool FormatExceptions
        {
            get
            {
                return m_FormatExceptions;
            }
            set
            {
                m_FormatExceptions = value;
                ExceptionFilters.Instance.FlagFormatExceptions = value;
            }
        }

        [Category("Optional exceptions to track")]
        [DisplayName("System.ArgumentException")]
        [Description("ArgumentException and derived exception classes")]
        public bool ArgumentException
        {
            get
            {
                return m_ArgumentExceptions;
            }
            set
            {
                m_ArgumentExceptions = value;
                ExceptionFilters.Instance.FlagArgumentExceptions = value;
            }
        }

        [Category("Optional exceptions to track")]
        [DisplayName("System.OverflowException")]
        [Description("OverflowException and derived exception classes")]
        public bool OverflowException
        {
            get
            {
                return m_OverflowExceptions;
            }
            set
            {
                m_OverflowExceptions = value;
                ExceptionFilters.Instance.FlagOverflowExceptions = value;
            }
        }

        [Category("Optional exceptions to track")]
        [DisplayName("Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException")]
        [Description("AssertFailedException and derived exception classes")]
        public bool AssertFailedException
        {
            get
            {
                return m_AssertFailedExceptions;
            }
            set
            {
                m_AssertFailedExceptions = value;
                ExceptionFilters.Instance.FlagAssertFailedExceptions = value;
            }
        }

        [Category("Optional exceptions to track")]
        [DisplayName("System.NotSupportedException")]
        [Description("NotSupportedException and derived exception classes")]
        public bool NotSupportedException
        {
            get
            {
                return m_NotSupportedExceptions;
            }
            set
            {
                m_NotSupportedExceptions = value;
                ExceptionFilters.Instance.FlagNotSupportedExceptions = value;
            }
        }

        [Category("Optional exceptions to track")]
        [DisplayName("System.NotImplementedException")]
        [Description("NotImplementedException and derived exception classes")]
        public bool NotImplementedException
        {
            get
            {
                return m_NotImplementedExceptions;
            }
            set
            {
                m_NotImplementedExceptions = value;
                ExceptionFilters.Instance.FlagNotImplementedExceptions = value;
            }
        }
    }
}

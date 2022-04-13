using CheckedExceptions;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace CheckedExceptionsOptions
{
    class CommentOptionsPage : DialogPage
    {
        private bool m_ShortCommentsForPrivate;
        private bool m_ShortCommentsForProtected;
        private bool m_ShortCommentsForInternal;
        private bool m_ShortCommentsForPublic;

        [Category("Code fix options")]
        [DisplayName("Short comment format for private methods")]
        [Description("Comment format includes only exceptions for private methods")]
        public bool ShortCommentsForPrivate
        {
            get
            {
                return m_ShortCommentsForPrivate;
            }
            set
            {
                m_ShortCommentsForPrivate = value;
                ShortCommentConfiguration.Instance.ForPrivate = value;
            }
        }

        [Category("Code fix options")]
        [DisplayName("Short comment format for protected methods")]
        [Description("Comment format includes only exceptions for protected methods")]
        public bool ShortCommentsForProtected
        {
            get
            {
                return m_ShortCommentsForProtected;
            }
            set
            {
                m_ShortCommentsForProtected = value;
                ShortCommentConfiguration.Instance.ForProtected = value;
            }
        }

        [Category("Code fix options")]
        [DisplayName("Short comment format for internal methods")]
        [Description("Comment format includes only exceptions for internal methods")]
        public bool ShortCommentsForInternal
        {
            get
            {
                return m_ShortCommentsForInternal;
            }
            set
            {
                m_ShortCommentsForInternal = value;
                ShortCommentConfiguration.Instance.ForInternal = value;
            }
        }

        [Category("Code fix options")]
        [DisplayName("Short comment format for public methods")]
        [Description("Comment format includes only exceptions for public methods")]
        public bool ShortCommentsForPublic
        {
            get
            {
                return m_ShortCommentsForPublic;
            }
            set
            {
                m_ShortCommentsForPublic = value;
                ShortCommentConfiguration.Instance.ForPublic = value;
            }
        }

    }
}

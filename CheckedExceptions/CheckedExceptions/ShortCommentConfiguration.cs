namespace CheckedExceptions
{
    public class ShortCommentConfiguration
    {
        private static ShortCommentConfiguration s_Instance = new ShortCommentConfiguration();

        public static ShortCommentConfiguration Instance
        {
            get { return s_Instance; }
        }

        private ShortCommentConfiguration() { }

        public bool ForPrivate { get; set; }
        public bool ForProtected { get; set; }
        public bool ForPublic { get; set; }
        public bool ForInternal { get; set; }
    }
}

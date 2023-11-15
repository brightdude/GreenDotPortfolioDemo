namespace Breezy.Muticaster
{
    public class BasicCredential
    {
        public static readonly BasicCredential None = new();

        public string Username { get; set; }

        public string Password { get; set; }

        internal bool IsNone => this.Equals(None);
    }
}
namespace Breezy.Muticaster
{
    internal class RequiredPermissionAttribute: System.Attribute
    {
        public RequiredPermissionAttribute(params string[] permissions)
        {
            AcceptedPermissions = permissions;
        }

        public string[] AcceptedPermissions { get; private set; }
    }
}

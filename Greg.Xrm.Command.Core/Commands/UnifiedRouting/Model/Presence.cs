namespace Greg.Xrm.Command.Commands.UnifiedRouting.Model
{
#pragma warning disable IDE1006 // Naming Styles
    public static class msdyn_presence
    {
        public static string msdyn_presenceid => "msdyn_presenceid";
        public static string msdyn_presencestatustext => "msdyn_presencestatustext";
        public static string msdyn_basepresencestatus => "msdyn_basepresencestatus";


        public enum AgentStatuses
        {
            Available = 192360000,
            Busy = 192360001,
            BusyDND = 192360002,
            Away = 192360003
        }
    }
}

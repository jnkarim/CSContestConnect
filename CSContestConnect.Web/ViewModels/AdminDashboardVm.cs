namespace CSContestConnect.Web.ViewModels
{
    using CSContestConnect.Web.Models;
    public class AdminDashboardVm
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int NewThisWeek { get; set; }
        public int ApprovedThisWeek { get; set; }
        public List<Event> Latest { get; set; } = new();
        public List<Event> UpcomingApproved { get; set; } = new();
    }
}

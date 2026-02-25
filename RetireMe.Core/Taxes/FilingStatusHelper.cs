namespace RetireMe.Core.Taxes
{
    public static class FilingStatusHelper
    {
        public static FilingStatus GetFilingStatusForYear(
            int yearOffset,
            SocialSecuritySettings primary,
            SocialSecuritySettings? spouse)
        {
            bool primaryAlive = (primary.CurrentAge + yearOffset) <= primary.DeathAge;
            bool spouseAlive = spouse != null && (spouse.CurrentAge + yearOffset) <= spouse.DeathAge;

            if (primaryAlive && spouseAlive)
                return FilingStatus.MarriedFilingJointly;

            if (primaryAlive || spouseAlive)
                return FilingStatus.Single;

            return FilingStatus.Single;
        }
    }
}


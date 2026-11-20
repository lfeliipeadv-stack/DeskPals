namespace DesktopPet
{
    /// <summary>
    /// Online addresses used by the application, all in one place.
    /// Updates and the pet gallery come only from <see cref="RepoUrl"/>.
    /// </summary>
    internal static class AppInfo
    {
        /// <summary>GitHub owner of the repository.</summary>
        public const string RepoOwner = "lfeliipeadv-stack";
        /// <summary>GitHub repository.</summary>
        public const string RepoName = "Desktop-Pet";
        /// <summary>Branch holding the pet gallery (Pets folder).</summary>
        public const string Branch = "master";

        /// <summary>Web page of the repository.</summary>
        public const string RepoUrl = "https://github.com/" + RepoOwner + "/" + RepoName;
        /// <summary>Pet gallery files (pets.json, folder/animations.xml, folder/icon.png …).</summary>
        public const string PetsRawUrl = "https://raw.githubusercontent.com/" + RepoOwner + "/" + RepoName + "/" + Branch + "/Pets/";
        /// <summary>Languages to download in the settings (languages.json, xx.json …).</summary>
        public const string LanguagesRawUrl = "https://raw.githubusercontent.com/" + RepoOwner + "/" + RepoName + "/" + Branch + "/Languages/";
        /// <summary>Pet gallery as a web page.</summary>
        public const string PetsWebUrl = RepoUrl + "/tree/" + Branch + "/Pets";
        /// <summary>GitHub API: latest release.</summary>
        public const string LatestReleaseApi = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";
        /// <summary>The project page (download, demo and the manual).</summary>
        public const string SiteUrl = "https://" + RepoOwner + ".github.io/" + RepoName + "/";
        /// <summary>Help: the manual (Wiki) on the project page.</summary>
        public const string HelpUrl = SiteUrl + "wiki/Home.html";
        /// <summary>Where a pet is sent to the gallery (a new issue with the form for pets; the .zip goes attached).</summary>
        public const string PetSubmitUrl = RepoUrl + "/issues/new?template=new_pet.md&labels=pet";
    }
}

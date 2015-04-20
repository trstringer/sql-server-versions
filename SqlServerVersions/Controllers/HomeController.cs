﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SqlServerVersions.Models;
using SqlServerVersions.ViewModels;

namespace SqlServerVersions.Controllers
{
    public class HomeController : Controller
    {
        private const int _topRecentCount = 10;
        private const int _allItemsIndex = -1;
        private const string _allItemsName = "<ALL>";
        private IEnumerable<VersionInfo> _baseMajorMinorVersions;

        public HomeController()
        {
            DataAccess dataAccess = new DataAccess();
            _baseMajorMinorVersions = dataAccess.GetMajorMinorReleases();
        }

        public ActionResult Index()
        {
            ViewBag.Title = "SQL Server Versions";

            return View();
        }

        [HttpPost]
        public ActionResult VersionSearch(VersionInfo versionInfo)
        {
            if (ModelState.IsValid) 
            {
                return RedirectToAction(
                    "VersionSearch",
                    new
                    {
                        major = versionInfo.Major,
                        minor = versionInfo.Minor,
                        build = versionInfo.Build,
                        revision = versionInfo.Revision
                    });
            }
            else
            {
                return View("Index");
            }
        }

        [HttpGet]
        public ActionResult VersionSearch(int major, int minor, int build, int revision)
        {
            VersionSearchViewModel versionSearchViewModel = new VersionSearchViewModel();
            
            versionSearchViewModel.FoundVersion = (new DataAccess()).GetVersionInfo(major, minor, build, revision);
            versionSearchViewModel.IsSearchedFor = true;

            return View("Index", versionSearchViewModel);
        }

        [HttpGet]
        public ActionResult RecentRelease(int major, int minor)
        {
            int PreSelectedId;
            DataAccess dataAccess = new DataAccess();
            List<VersionInfo> MajorMinorReleasesList = _baseMajorMinorVersions.ToList();

            MajorMinorReleasesList.Insert(
                0, 
                new VersionInfo()
                {
                    Id = _allItemsIndex,
                    Major = 0,
                    Minor = 0,
                    Build = 0,
                    Revision = 0,
                    FriendlyNameShort = _allItemsName
                });

            PreSelectedId = MajorMinorReleasesList.First(m => m.Major == major && m.Minor == minor).Id;

            return View(
                new RecentReleaseViewModel()
                {
                    MajorMinorBaseVersions = MajorMinorReleasesList,
                    RecentVersions = dataAccess.GetTopRecentReleaseVersionInfo(_topRecentCount, major, minor),
                    SelectedId = PreSelectedId
                });
        }

        [HttpPost]
        public ActionResult RecentRelease(RecentReleaseViewModel recentReleaseViewModel)
        {
            int major, minor;

            if (recentReleaseViewModel.SelectedId == -1)
            {
                major = 0;
                minor = 0;
            }
            else
            {
                VersionInfo SelectedBaseVersion = _baseMajorMinorVersions.First(m => m.Id == recentReleaseViewModel.SelectedId);
                major = SelectedBaseVersion.Major;
                minor = SelectedBaseVersion.Minor;
            }

            return RedirectToAction("RecentRelease", new { major = major, minor = minor });
        }

        [HttpGet]
        public ActionResult Supportability(int major, int minor)
        {
            int PreSelectedId;
            DataAccess dataAccess = new DataAccess();
            List<VersionInfo> MajorMinorReleasesList = _baseMajorMinorVersions.ToList();

            MajorMinorReleasesList.Insert(
                0,
                new VersionInfo()
                {
                    Id = _allItemsIndex,
                    Major = 0,
                    Minor = 0,
                    Build = 0,
                    Revision = 0,
                    FriendlyNameShort = _allItemsName
                });

            PreSelectedId = MajorMinorReleasesList.First(m => m.Major == major && m.Minor == minor).Id;

            List<SupportabilityBoundaries> Boundaries = new List<SupportabilityBoundaries>();

            // if the major and minor are set to zero then the user wants to display all 
            // and in this case we will need to populate the collection with all versions
            //
            // otherwise just add the particular boundary value if we are specifiy (i.e. 
            // major and minor are not zero)
            //
            if (major == 0 && minor == 0)
                foreach (VersionInfo majorMinorVersion in _baseMajorMinorVersions)
                    Boundaries.Add(GetBoundaries(majorMinorVersion));
            else
                Boundaries.Add(GetBoundaries(MajorMinorReleasesList.First(m => m.Major == major && m.Minor == minor)));

            return View(
                new SupportabilityViewModel()
                {
                    MajorMinorBaseVersions = MajorMinorReleasesList,
                    SelectedId = PreSelectedId,
                    VersionBoundaries = Boundaries
                });
        }

        [HttpPost]
        public ActionResult Supportability(SupportabilityViewModel supportabilityViewModel)
        {
            int major, minor;

            if (supportabilityViewModel.SelectedId == -1)
            {
                major = 0;
                minor = 0;
            }
            else
            {
                VersionInfo SelectedBaseVersion = _baseMajorMinorVersions.First(m => m.Id == supportabilityViewModel.SelectedId);
                major = SelectedBaseVersion.Major;
                minor = SelectedBaseVersion.Minor;
            }

            return RedirectToAction("Supportability", new { major = major, minor = minor });
        }

        private SupportabilityBoundaries GetBoundaries(VersionInfo baseVersion)
        {
            DataAccess dataAccess = new DataAccess();
            VersionInfo LowestSupported, HighestSupported;
            IEnumerable<VersionInfo> VersionBoundaries = dataAccess.GetRecentAndOldestSupportedVersions().Where(m => m.Major == baseVersion.Major && m.Minor == baseVersion.Minor);

            if (VersionBoundaries.Count() > 0)
            {
                LowestSupported = VersionBoundaries.OrderBy(m => m.ReleaseDate).First();
                HighestSupported = VersionBoundaries.OrderByDescending(m => m.ReleaseDate).First();
            }
            else
            {
                LowestSupported = null;
                HighestSupported = null;
            }

            return new SupportabilityBoundaries()
            {
                BaseVersion = baseVersion,
                OldestSupported = LowestSupported,
                NewestSupported = HighestSupported
            };
        }
    }
}
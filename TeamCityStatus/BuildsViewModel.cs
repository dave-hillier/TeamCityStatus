using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Windows.Threading;
using System.Diagnostics;
using System.Reactive.Concurrency;

namespace TeamCityStatus
{
    enum BuildStatus
    {
        Success, Failure, Error, Running, Failing, Unknown
    }

    class BuildViewModel
    {
        private readonly IDisposable _sub;
        public BuildViewModel(string project, IObservable<BuildStepViewModel> steps)
        {
            Project = project;
            Steps = new ObservableCollection<BuildStepViewModel>();
            _sub = steps.ObserveOnDispatcher().Subscribe(s => Steps.Add(s));
        }

        public string Project { get; private set; }
        public ObservableCollection<BuildStepViewModel> Steps { get; private set; }
    }

    class BuildsViewModel
    {
        private ObservableCollection<BuildViewModel> _builds = new ObservableCollection<BuildViewModel>();
        private IDisposable _sub;

        private static BuildStatus ParseTeamCityStatus(string statusString)
        {
            BuildStatus bs = BuildStatus.Unknown;
            if (statusString == "SUCCESS")
                bs = BuildStatus.Success;
            else if (statusString == "FAILURE")
                bs = BuildStatus.Failure;
            else if (statusString == "ERROR")
                bs = BuildStatus.Error;
            return bs;
        }

        public BuildsViewModel()
        {
            // http://teamcity.jetbrains.com/guestAuth/app/rest/buildTypes
            var dispatcher = Dispatcher.CurrentDispatcher;
            var baseUri = new Uri("http://teamcity.jetbrains.com");

            // Get the state of the world
            var buildTypesResponse = from buildTypesXmlResponse in AsyncWebRequest.GetXml(new Uri(baseUri, "/guestAuth/app/rest/buildTypes"))
                                     from buildTypeElement in buildTypesXmlResponse.Element("buildTypes").Elements("buildType")
                                     let name = buildTypeElement.Attribute("projectName").Value
                                     select new
                                     {
                                         BuildName = buildTypeElement.Attribute("name").Value,
                                         ProjectName = buildTypeElement.Attribute("projectName").Value,
                                         Href = buildTypeElement.Attribute("href").Value,
                                         Id = buildTypeElement.Attribute("id").Value
                                     };

            var buildXmlResponses = from buildType in buildTypesResponse
                                    from buildXml in AsyncWebRequest.GetXml(new Uri(baseUri, "/guestAuth/app/rest/builds/buildType:" + buildType.Id))
                                    select new { Type = buildType, Xml = buildXml };
             
            var buildStepViewModels = from response in buildXmlResponses
                             let buildElement = response.Xml.Element("build")
                             let status = ExtractBuildStatus(buildElement)
                             let users = status == BuildStatus.Success ? 
                                Observable.Empty<string>() : GetUsersForBuild(baseUri, buildElement)
                             select new BuildStepViewModel(users, dispatcher)
                             {
                                 Project = response.Type.ProjectName,
                                 BuildName = response.Type.BuildName,
                                 Status = status,
                                 BuildTypeId = response.Type.Id,
                             };

            var grouped = from step in buildStepViewModels
                          group step by step.Project into projectSteps
                          select projectSteps;

            _sub = grouped.ObserveOn(dispatcher).Subscribe(
                g => _builds.Add(new BuildViewModel(g.Key, g.Select(p => p))));

            // TODO: poll running builds "/guestAuth/app/rest/builds?locator=running:true"
            // TODO: update the builds as appropriate
            // TODO: update the build queue
        }

        private static IObservable<string> GetUsersForBuild(Uri baseUri, System.Xml.Linq.XElement buildElement)
        {
            Debug.WriteLine("Users {0}", baseUri);
            var changesListHref = buildElement.Element("changes").Attribute("href").Value;

            var buildChangesResponse =
                from changesXDoc in AsyncWebRequest.GetXml(new Uri(baseUri, changesListHref))
                from changeElement in changesXDoc.Element("changes").Elements("change")
                select changeElement.Attribute("href").Value;

            var changeUsernamesRequest =
                from buildChangeHrefs in buildChangesResponse
                from response in AsyncWebRequest.GetXml(new Uri(baseUri, buildChangeHrefs))
                select response.Element("change").Attribute("username").Value;
            return changeUsernamesRequest;
        }

        private static BuildStatus ExtractBuildStatus(System.Xml.Linq.XElement buildElement)
        {
            var statusString = buildElement.Attribute("status").Value;
            BuildStatus bs = ParseTeamCityStatus(statusString);
            return bs;
        }

        public ObservableCollection<BuildViewModel> Builds { get { return _builds; } }
    }
}

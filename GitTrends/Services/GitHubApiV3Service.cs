﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitTrends.Mobile.Shared;
using GitTrends.Shared;
using Refit;

namespace GitTrends
{
    public class GitHubApiV3Service : BaseMobileApiService
    {
        readonly static Lazy<IGitHubApiV3> _githubApiClient = new Lazy<IGitHubApiV3>(() => RestService.For<IGitHubApiV3>(CreateHttpClient(GitHubConstants.GitHubRestApiUrl)));

        static IGitHubApiV3 GithubApiClient => _githubApiClient.Value;

        public async IAsyncEnumerable<Repository> UpdateRepositoriesWithViewsAndClonesData(List<Repository> repositories)
        {
            var getRepositoryStatisticsTaskList = new List<Task<(RepositoryViewsResponseModel, RepositoryClonesResponseModel)>>(repositories.Select(x => getRepositoryStatistics(x)));

            while (getRepositoryStatisticsTaskList.Any())
            {
                var completedStatisticsTask = await Task.WhenAny(getRepositoryStatisticsTaskList).ConfigureAwait(false);
                getRepositoryStatisticsTaskList.Remove(completedStatisticsTask);

                var (viewsResponse, clonesResponse) = await completedStatisticsTask.ConfigureAwait(false);

                var matchingRepository = repositories.First(x => x.Name == viewsResponse.RepositoryName);


                yield return new Repository(matchingRepository.Name, matchingRepository.Description, matchingRepository.ForkCount,
                                            new RepositoryOwner(matchingRepository.OwnerLogin, matchingRepository.OwnerAvatarUrl),
                                            new IssuesConnection(matchingRepository.IssuesCount, null),
                                            matchingRepository.Url,
                                            new StarGazers(matchingRepository.StarCount),
                                            matchingRepository.IsFork,
                                            viewsResponse.DailyViewsList,
                                            clonesResponse.DailyClonesList);
            }

            async Task<(RepositoryViewsResponseModel ViewsResponse, RepositoryClonesResponseModel ClonesResponse)> getRepositoryStatistics(Repository repository)
            {
                var getViewStatisticsTask = GetRepositoryViewStatistics(repository.OwnerLogin, repository.Name);
                var getCloneStatisticsTask = GetRepositoryCloneStatistics(repository.OwnerLogin, repository.Name);

                await Task.WhenAll(getViewStatisticsTask, getCloneStatisticsTask).ConfigureAwait(false);

                return (await getViewStatisticsTask.ConfigureAwait(false),
                        await getCloneStatisticsTask.ConfigureAwait(false));
            }
        }

        public async Task<RepositoryViewsResponseModel> GetRepositoryViewStatistics(string owner, string repo, CancellationToken cancellationToken = default)
        {
            if (GitHubAuthenticationService.IsDemoUser)
            {
                //Yield off of the main thread to generate dailyViewsModelList
                await Task.Yield();

                var dailyViewsModelList = new List<DailyViewsModel>();

                for (int i = 1; i < 14; i++)
                {
                    var count = DemoDataConstants.GetRandomNumber();
                    var uniqeCount = count / 2; //Ensures uniqueCount is always less than count

                    dailyViewsModelList.Add(new DailyViewsModel(DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(i)), count, uniqeCount));
                }

                //Add one trending repo
                dailyViewsModelList.Add(new DailyViewsModel(DateTimeOffset.UtcNow, DemoDataConstants.MaximumRandomNumber * 4, DemoDataConstants.MaximumRandomNumber / 2));

                return new RepositoryViewsResponseModel(repo, owner, dailyViewsModelList.Sum(x => x.TotalViews), dailyViewsModelList.Sum(x => x.TotalUniqueViews), dailyViewsModelList);
            }
            else
            {
                var token = await GitHubAuthenticationService.GetGitHubToken().ConfigureAwait(false);
                var response = await AttemptAndRetry_Mobile(() => GithubApiClient.GetRepositoryViewStatistics(owner, repo, GetGitHubBearerTokenHeader(token))).ConfigureAwait(false);

                return new RepositoryViewsResponseModel(repo, owner, response.TotalCount, response.TotalUniqueCount, response.DailyViewsList);
            }
        }

        public async Task<RepositoryClonesResponseModel> GetRepositoryCloneStatistics(string owner, string repo, CancellationToken cancellationToken = default)
        {
            if (GitHubAuthenticationService.IsDemoUser)
            {
                //Yield off of the main thread to generate dailyViewsModelList
                await Task.Yield();

                var dailyViewsModelList = new List<DailyClonesModel>();

                for (int i = 0; i < 14; i++)
                {
                    var count = DemoDataConstants.GetRandomNumber() / 2; //Ensures the average clone count is smaller than the average view count
                    var uniqeCount = count / 2; //Ensures uniqueCount is always less than count

                    dailyViewsModelList.Add(new DailyClonesModel(DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(i)), count, uniqeCount));
                }

                return new RepositoryClonesResponseModel(repo, owner, dailyViewsModelList.Sum(x => x.TotalClones), dailyViewsModelList.Sum(x => x.TotalUniqueClones), dailyViewsModelList);
            }
            else
            {
                var token = await GitHubAuthenticationService.GetGitHubToken().ConfigureAwait(false);
                var response = await AttemptAndRetry_Mobile(() => GithubApiClient.GetRepositoryCloneStatistics(owner, repo, GetGitHubBearerTokenHeader(token))).ConfigureAwait(false);

                return new RepositoryClonesResponseModel(repo, owner, response.TotalCount, response.TotalUniqueCount, response.DailyClonesList);
            }
        }

        public async Task<List<ReferringSiteModel>> GetReferringSites(string owner, string repo, CancellationToken cancellationToken = default)
        {
            if (GitHubAuthenticationService.IsDemoUser)
            {
                //Yield off of main thread to generate MobileReferringSiteModels
                await Task.Yield();

                var referringSitesList = new List<ReferringSiteModel>();

                for (int i = 0; i < DemoDataConstants.ReferringSitesCount; i++)
                {
                    referringSitesList.Add(new ReferringSiteModel(DemoDataConstants.GetRandomNumber(), DemoDataConstants.GetRandomNumber(), DemoDataConstants.GetRandomText()));
                }

                return referringSitesList;
            }
            else
            {
                var token = await GitHubAuthenticationService.GetGitHubToken().ConfigureAwait(false);
                var referringSites = await AttemptAndRetry_Mobile(() => GithubApiClient.GetReferingSites(owner, repo, GetGitHubBearerTokenHeader(token))).ConfigureAwait(false);

                return referringSites;
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Windows;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    [Export(typeof(IDetailsService))]
    internal class DetailsController : IDetailsService
    {
        private readonly ISelectionService _selectionService;
        private readonly IShellService _shellService;

        private Lazy<IDetailsViewModel> _detailsViewModel;

        private readonly HashSet<IBlog> _blogsToSave;

        [ImportingConstructor]
        public DetailsController(IShellService shellService, ISelectionService selectionService, IManagerService managerService)
        {
            _shellService = shellService;
            _selectionService = selectionService;
            _blogsToSave = new HashSet<IBlog>();
        }

        public QueueManager QueueManager { get; set; }

        [ImportMany(typeof(IDetailsViewModel))]
        private IEnumerable<Lazy<IDetailsViewModel, ICrawlerData>> ViewModelFactoryLazy { get; set; }

        public Lazy<IDetailsViewModel> GetViewModel(IBlog blog)
        {
            Lazy<IDetailsViewModel, ICrawlerData> viewModel =
                ViewModelFactoryLazy.FirstOrDefault(list => list.Metadata.BlogType == blog.GetType());

            if (viewModel == null)
            {
                throw new ArgumentException("Website is not supported!", nameof(blog));
            }

            return viewModel;
        }

        private IDetailsViewModel DetailsViewModel => _detailsViewModel.Value;

        public void UpdateBlogPreview(IReadOnlyList<IBlog> blogFiles)
        {
            if (DetailsViewModel?.BlogFile?.SettingsTabIndex == 2)
            {
                DetailsViewModel.BlogFile.PropertyChanged -= ChangeBlogSettings;
                SelectBlogFiles(blogFiles, true);
            }
        }

        public void SelectBlogFiles(IReadOnlyList<IBlog> blogFiles, bool showPreview)
        {
            UpdateViewModelBasedOnSelection(blogFiles);

            ClearBlogSelection();

            if (blogFiles.Count <= 1 || showPreview && _shellService.Settings.EnablePreview)
            {
                DetailsViewModel.Count = 1;
                DetailsViewModel.BlogFile = blogFiles.FirstOrDefault();
                if (DetailsViewModel.BlogFile != null)
                    DetailsViewModel.BlogFile.SettingsTabIndex = (showPreview && _shellService.Settings.EnablePreview) ? 2 : DetailsViewModel.BlogFile.SettingsTabIndex;
            }
            else
            {
                DetailsViewModel.Count = blogFiles.Count;
                DetailsViewModel.BlogFile = CreateFromMultiple(blogFiles.ToArray());
                DetailsViewModel.BlogFile.PropertyChanged += ChangeBlogSettings;
            }
        }

        public bool FilenameTemplateValidate(string enteredFilenameTemplate)
        {
            if (string.IsNullOrEmpty(enteredFilenameTemplate) || enteredFilenameTemplate == "%f") return true;
            //var tokens = new List<string>() { "%f", "%d", "%p", "%i", "%s" };
            //if (!tokens.Any(x => enteredFilenameTemplate.IndexOf(x, StringComparison.InvariantCultureIgnoreCase) >= 0))
            //{
            //    MessageBox.Show(Resources.FilenameTemplateTokenNotFound, Resources.Warning);
            //    return false;
            //}
            var needed = new List<string>() { "%x", "%y" };
            if (enteredFilenameTemplate.IndexOf("%f", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                !needed.Any(x => enteredFilenameTemplate.IndexOf(x, StringComparison.InvariantCultureIgnoreCase) >= 0))
            {
                MessageBox.Show(Resources.FilenameTemplateAppendTokenNotFound, Resources.Warning);
                return false;
            }
            return true;
        }

        private void UpdateViewModelBasedOnSelection(IReadOnlyList<IBlog> blogFiles)
        {
            if (blogFiles.Count == 0)
            {
                return;
            }

            _detailsViewModel = GetViewModel(blogFiles.Select(blog => blog.GetType()).Distinct().Count() < 2
                ? blogFiles.FirstOrDefault()
                : new Blog());
            _shellService.DetailsView = DetailsViewModel.View;
            _shellService.UpdateDetailsView();
        }

        private void ChangeBlogSettings(object sender, PropertyChangedEventArgs e)
        {
            foreach (IBlog blog in _blogsToSave)
            {
                PropertyInfo property = typeof(IBlog).GetProperty(e.PropertyName);
                if (CheckIfCanUpdateTumblrBlogCrawler(blog, property))
                    continue;
                property.SetValue(blog, property.GetValue(DetailsViewModel.BlogFile));
            }
        }

        public void Initialize()
        {
            ((INotifyCollectionChanged)_selectionService.SelectedBlogFiles).CollectionChanged += SelectedBlogFilesCollectionChanged;
            _detailsViewModel = GetViewModel(new Blog());
            _shellService.DetailsView = DetailsViewModel.View;
        }

        public void Shutdown()
        {
        }

        public IBlog CreateFromMultiple(IEnumerable<IBlog> blogFiles)
        {
            List<IBlog> sharedBlogFiles = blogFiles.ToList();
            if (!sharedBlogFiles.Any())
            {
                throw new ArgumentException("The collection must have at least one item.", nameof(blogFiles));
            }

            foreach (IBlog blog in sharedBlogFiles)
            {
                _blogsToSave.Add(blog);
            }

            return new Blog()
            {
                Name = string.Join(", ", sharedBlogFiles.Select(blog => blog.Name).ToArray()),
                Url = string.Join(", ", sharedBlogFiles.Select(blog => blog.Url).ToArray()),
                Posts = sharedBlogFiles.Sum(blogs => blogs.Posts),
                TotalCount = sharedBlogFiles.Sum(blogs => blogs.TotalCount),
                Texts = sharedBlogFiles.Sum(blogs => blogs.Texts),
                Answers = sharedBlogFiles.Sum(blogs => blogs.Answers),
                Quotes = sharedBlogFiles.Sum(blogs => blogs.Quotes),
                Photos = sharedBlogFiles.Sum(blogs => blogs.Photos),
                NumberOfLinks = sharedBlogFiles.Sum(blogs => blogs.NumberOfLinks),
                Conversations = sharedBlogFiles.Sum(blogs => blogs.Conversations),
                Videos = sharedBlogFiles.Sum(blogs => blogs.Videos),
                Audios = sharedBlogFiles.Sum(blogs => blogs.Audios),
                PhotoMetas = sharedBlogFiles.Sum(blogs => blogs.PhotoMetas),
                VideoMetas = sharedBlogFiles.Sum(blogs => blogs.VideoMetas),
                AudioMetas = sharedBlogFiles.Sum(blogs => blogs.AudioMetas),
                DownloadedTexts = sharedBlogFiles.Sum(blogs => blogs.DownloadedTexts),
                DownloadedQuotes = sharedBlogFiles.Sum(blogs => blogs.DownloadedQuotes),
                DownloadedPhotos = sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotos),
                DownloadedLinks = sharedBlogFiles.Sum(blogs => blogs.DownloadedLinks),
                DownloadedConversations = sharedBlogFiles.Sum(blogs => blogs.DownloadedConversations),
                DownloadedAnswers = sharedBlogFiles.Sum(blogs => blogs.DownloadedAnswers),
                DownloadedVideos = sharedBlogFiles.Sum(blogs => blogs.DownloadedVideos),
                DownloadedAudios = sharedBlogFiles.Sum(blogs => blogs.DownloadedAudios),
                DownloadedPhotoMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotoMetas),
                DownloadedVideoMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedVideoMetas),
                DownloadedAudioMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedAudioMetas),
                DownloadPages = SetProperty<string>(sharedBlogFiles, "DownloadPages"),
                PageSize = SetProperty<int>(sharedBlogFiles, "PageSize"),
                DownloadFrom = SetProperty<string>(sharedBlogFiles, "DownloadFrom"),
                DownloadTo = SetProperty<string>(sharedBlogFiles, "DownloadTo"),
                Tags = SetProperty<string>(sharedBlogFiles, "Tags"),
                Password = SetProperty<string>(sharedBlogFiles, "Password"),
                DownloadAudio = SetCheckBox(sharedBlogFiles, "DownloadAudio"),
                DownloadConversation = SetCheckBox(sharedBlogFiles, "DownloadConversation"),
                DownloadLink = SetCheckBox(sharedBlogFiles, "DownloadLink"),
                DownloadPhoto = SetCheckBox(sharedBlogFiles, "DownloadPhoto"),
                DownloadQuote = SetCheckBox(sharedBlogFiles, "DownloadQuote"),
                DownloadText = SetCheckBox(sharedBlogFiles, "DownloadText"),
                DownloadAnswer = SetCheckBox(sharedBlogFiles, "DownloadAnswer"),
                DownloadVideo = SetCheckBox(sharedBlogFiles, "DownloadVideo"),
                CreatePhotoMeta = SetCheckBox(sharedBlogFiles, "CreatePhotoMeta"),
                CreateVideoMeta = SetCheckBox(sharedBlogFiles, "CreateVideoMeta"),
                CreateAudioMeta = SetCheckBox(sharedBlogFiles, "CreateAudioMeta"),
                DownloadRebloggedPosts = SetCheckBox(sharedBlogFiles, "DownloadRebloggedPosts"),
                SkipGif = SetCheckBox(sharedBlogFiles, "SkipGif"),
                ForceSize = SetCheckBox(sharedBlogFiles, "ForceSize"),
                ForceRescan = SetCheckBox(sharedBlogFiles, "ForceRescan"),
                CheckDirectoryForFiles = SetCheckBox(sharedBlogFiles, "CheckDirectoryForFiles"),
                DownloadUrlList = SetCheckBox(sharedBlogFiles, "DownloadUrlList"),
                SettingsTabIndex = SetProperty<int>(sharedBlogFiles, "SettingsTabIndex"),
                DownloadImgur = SetCheckBox(sharedBlogFiles, "DownloadImgur"),
                DownloadGfycat = SetCheckBox(sharedBlogFiles, "DownloadGfycat"),
                DownloadWebmshare = SetCheckBox(sharedBlogFiles, "DownloadWebmshare"),
                DownloadMixtape = SetCheckBox(sharedBlogFiles, "DownloadMixtape"),
                DownloadUguu = SetCheckBox(sharedBlogFiles, "DownloadUguu"),
                DownloadSafeMoe = SetCheckBox(sharedBlogFiles, "DownloadSafeMoe"),
                DownloadLoliSafe = SetCheckBox(sharedBlogFiles, "DownloadLoliSafe"),
                DownloadCatBox = SetCheckBox(sharedBlogFiles, "DownloadCatBox"),
                GfycatType = SetProperty<GfycatTypes>(sharedBlogFiles, "GfycatType"),
                WebmshareType = SetProperty<WebmshareTypes>(sharedBlogFiles, "WebmshareType"),
                MixtapeType = SetProperty<MixtapeTypes>(sharedBlogFiles, "MixtapeType"),
                UguuType = SetProperty<UguuTypes>(sharedBlogFiles, "UguuType"),
                SafeMoeType = SetProperty<SafeMoeTypes>(sharedBlogFiles, "SafeMoeType"),
                LoliSafeType = SetProperty<LoliSafeTypes>(sharedBlogFiles, "LoliSafeType"),
                CatBoxType = SetProperty<CatBoxTypes>(sharedBlogFiles, "CatBoxType"),
                MetadataFormat = SetProperty<MetadataType>(sharedBlogFiles, "MetadataFormat"),
                BlogType = SetProperty<BlogTypes>(sharedBlogFiles, "BlogType"),
                DumpCrawlerData = SetCheckBox(sharedBlogFiles, "DumpCrawlerData"),
                RegExPhotos = SetCheckBox(sharedBlogFiles, "RegExPhotos"),
                RegExVideos = SetCheckBox(sharedBlogFiles, "RegExVideos"),
                FileDownloadLocation = SetProperty<string>(sharedBlogFiles, "FileDownloadLocation"),
                Dirty = false
            };
        }

        private static T SetProperty<T>(IReadOnlyCollection<IBlog> blogs, string propertyName) where T : IConvertible
        {
            PropertyInfo property = typeof(IBlog).GetProperty(propertyName);
            var value = (T)property.GetValue(blogs.FirstOrDefault());
            if (value == null)
            {
                return default(T);
            }

            bool equal = blogs.All(blog => property.GetValue(blog)?.Equals(value) ?? false);
            return equal ? value : default(T);
        }

        private static bool SetCheckBox(IReadOnlyCollection<IBlog> blogs, string propertyName)
        {
            PropertyInfo property = typeof(IBlog).GetProperty(propertyName);

            int numberOfBlogs = blogs.Count;
            int checkedBlogs = blogs.Select(blog => (bool)property.GetValue(blog)).Count(state => state);

            return checkedBlogs == numberOfBlogs;
        }

        private static bool CheckIfCanUpdateTumblrBlogCrawler(IBlog blog, PropertyInfo property)
        {
            return property.PropertyType == typeof(BlogTypes) && !(blog is TumblrBlog || blog is TumblrHiddenBlog);
        }

        private void ClearBlogSelection()
        {
            if (_blogsToSave.Any())
            {
                _blogsToSave.Clear();
            }
        }

        private void SelectedBlogFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DetailsViewModel.BlogFile != null)
            {
                DetailsViewModel.BlogFile.PropertyChanged -= ChangeBlogSettings;
            }

            SelectBlogFiles(_selectionService.SelectedBlogFiles.ToArray(), false);
        }
    }
}

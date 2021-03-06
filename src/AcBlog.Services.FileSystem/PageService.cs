﻿using AcBlog.Data.Extensions;
using AcBlog.Data.Models;
using AcBlog.Data.Models.Actions;
using AcBlog.Data.Repositories;
using AcBlog.Data.Repositories.FileSystem.Readers;
using AcBlog.Data.Repositories.Searchers;
using StardustDL.Extensions.FileProviders;

namespace AcBlog.Services.FileSystem
{
    internal class PageService : RecordRepoBasedService<Page, string, PageQueryRequest, IPageRepository>, IPageService
    {
        public PageService(IBlogService blog, string rootPath, IFileProvider fileProvider) : base(blog, new PageFSReader(rootPath, fileProvider))
        {
        }
    }
}

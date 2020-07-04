﻿using AcBlog.Data.Repositories;
using AcBlog.Sdk;
using AcBlog.Sdk.API;
using AcBlog.Sdk.StaticFile;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AcBlog.Tools.Sdk.Models
{
    public class Workspace
    {
        public Workspace(DirectoryInfo root)
        {
            Root = root;
            DbRoot = new DirectoryInfo(Path.Join(Root.FullName, ".acblog"));
        }

        public WorkspaceConfiguration Configuration { get; private set; } = new WorkspaceConfiguration();

        public Db History { get; private set; } = new Db();

        public DirectoryInfo Root { get; }

        public DirectoryInfo DbRoot { get; }

        public IBlogService? Remote { get; private set; }

        public Task Connect(HttpClient httpClient)
        {
            if (Configuration.Remote is null)
                throw new Exception("No remote configured.");
            httpClient.BaseAddress = Configuration.Remote.Uri;
            if (Configuration.Remote.IsStatic)
            {
                Remote = new HttpStaticFileBlogService(Configuration.Remote.Uri.LocalPath, httpClient);
            }
            else
            {
                Remote = new ApiBlogService(httpClient);
            }
            return Task.CompletedTask;
        }

        public Task Login()
        {
            if (Remote is null)
                throw new Exception("Please connect first.");
            Remote.PostService.Context ??= new RepositoryAccessContext();
            Remote.KeywordService.Context ??= new RepositoryAccessContext();
            Remote.CategoryService.Context ??= new RepositoryAccessContext();
            Remote.UserService.Context ??= new RepositoryAccessContext();
            Remote.PostService.Context.Token = Configuration.Token;
            Remote.KeywordService.Context.Token = Configuration.Token;
            Remote.CategoryService.Context.Token = Configuration.Token;
            Remote.UserService.Context.Token = Configuration.Token;
            return Task.CompletedTask;
        }

        public async Task Load()
        {
            if (DbRoot.Exists)
            {
                {
                    var fileinfo = new FileInfo(Path.Join(DbRoot.FullName, "config.json"));
                    if (fileinfo.Exists)
                    {
                        using var fs = fileinfo.Open(FileMode.Open, FileAccess.Read);
                        Configuration = await JsonSerializer.DeserializeAsync<WorkspaceConfiguration>(fs);
                    }
                }
                {
                    var fileinfo = new FileInfo(Path.Join(DbRoot.FullName, "history.json"));
                    if (fileinfo.Exists)
                    {
                        using var fs = fileinfo.Open(FileMode.Open, FileAccess.Read);
                        History = await JsonSerializer.DeserializeAsync<Db>(fs);
                    }
                }
            }
        }

        public async Task Save()
        {
            DbRoot.Refresh();
            if (!DbRoot.Exists)
            {
                DbRoot.Create();
                DbRoot.Refresh();
            }
            {
                var fileinfo = new FileInfo(Path.Join(DbRoot.FullName, "config.json"));
                using var fs = fileinfo.Open(FileMode.Create, FileAccess.Write);
                await JsonSerializer.SerializeAsync(fs, Configuration);
            }
            {
                var fileinfo = new FileInfo(Path.Join(DbRoot.FullName, "history.json"));
                using var fs = fileinfo.Open(FileMode.Create, FileAccess.Write);
                await JsonSerializer.SerializeAsync(fs, History);
            }
        }
    }
}

﻿using AcBlog.Data.Models;
using AcBlog.Data.Models.Actions;
using AcBlog.Data.Repositories.SqlServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AcBlog.Data.Repositories.SqlServer
{
    public class PostRepository : IPostRepository
    {
        public PostRepository(DataContext data) => Data = data;

        readonly Lazy<RepositoryStatus> _status = new Lazy<RepositoryStatus>(new RepositoryStatus
        {
            CanRead = true,
            CanWrite = true,
        });

        DataContext Data { get; set; }

        public RepositoryAccessContext Context { get; set; } = new RepositoryAccessContext();

        public IAsyncEnumerable<string> All(CancellationToken cancellationToken = default) => Data.Posts.AsQueryable().Select(x => x.Id).AsAsyncEnumerable();

        public async Task<string> Create(Post value, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(value.Id))
                value.Id = Guid.NewGuid().ToString();
            Data.Posts.Add(RawPost.From(value));
            await Data.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return value.Id;
        }

        public async Task<bool> Delete(string id, CancellationToken cancellationToken = default)
        {
            var item = await Data.Posts.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
            if (item is null)
                return false;
            Data.Posts.Remove(item);
            await Data.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> Exists(string id, CancellationToken cancellationToken = default)
        {
            var item = await Data.Posts.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
            return item is not null;
        }

        public async Task<Post> Get(string id, CancellationToken cancellationToken = default)
        {
            var item = await Data.Posts.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
            return RawPost.To(item);
        }

        IQueryable<string> InnerQuery(PostQueryRequest query)
        {
            var qr = Data.Posts.AsQueryable();

            if (query.Type is not null)
                qr = qr.Where(x => x.Type == query.Type);
            if (!string.IsNullOrWhiteSpace(query.Author))
                qr = qr.Where(x => x.AuthorId == query.Author);
            if (query.Category is not null)
                qr = qr.Where(x => x.Category.StartsWith(query.Category.ToString()));
            if (query.Keywords is not null)
                qr = qr.Where(x => x.Keywords.StartsWith(query.Keywords.ToString()));
            if (!string.IsNullOrWhiteSpace(query.Title))
                qr = qr.Where(x => x.Title.Contains(query.Title));
            if (!string.IsNullOrWhiteSpace(query.Content))
            {
                string jsonContent = JsonSerializer.Serialize(query.Content);
                qr = qr.Where(x => x.Content.Contains(jsonContent));
            }
            qr = query.Order switch
            {
                PostQueryOrder.None => qr,
                PostQueryOrder.CreationTimeAscending => qr.OrderBy(x => x.CreationTime),
                PostQueryOrder.CreationTimeDescending => qr.OrderByDescending(x => x.CreationTime),
                PostQueryOrder.ModificationTimeAscending => qr.OrderBy(x => x.ModificationTime),
                PostQueryOrder.ModificationTimeDescending => qr.OrderByDescending(x => x.ModificationTime),
                _ => throw new NotImplementedException(),
            };

            if (query.Pagination is not null)
            {
                qr = qr.Skip(query.Pagination.Offset).Take(query.Pagination.PageSize);
            }

            return qr.Select(x => x.Id);
        }

        public IAsyncEnumerable<string> Query(PostQueryRequest query, CancellationToken cancellationToken = default)
        {
            return InnerQuery(query).AsAsyncEnumerable();
        }

        public async Task<QueryStatistic> Statistic(PostQueryRequest query, CancellationToken cancellationToken = default)
        {
            var count = await InnerQuery(query).CountAsync(cancellationToken);
            return new QueryStatistic
            {
                Count = count
            };
        }

        public async Task<bool> Update(Post value, CancellationToken cancellationToken = default)
        {
            var to = RawPost.From(value);

            var item = await Data.Posts.FindAsync(new object[] { to.Id }, cancellationToken).ConfigureAwait(false);
            if (item is null)
                return false;

            item.Keywords = to.Keywords;
            item.Category = to.Category;
            item.AuthorId = to.AuthorId;
            item.Content = to.Content;
            item.CreationTime = to.CreationTime;
            item.ModificationTime = to.ModificationTime;
            item.Title = to.Title;
            item.Type = to.Type;

            Data.Posts.Update(item);
            await Data.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        public Task<RepositoryStatus> GetStatus(CancellationToken cancellationToken = default) => Task.FromResult(_status.Value);

        public Task<CategoryTree> GetCategories(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<KeywordCollection> GetKeywords(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}

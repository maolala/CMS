﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Http;
using System.Web.Mvc;
using System.Linq;
using AtomLab.Core;
using Webdiyer.WebControls.Mvc;
using YangKai.BlogEngine.Common;
using YangKai.BlogEngine.Domain;
using YangKai.BlogEngine.Web.Mvc.Extension;

namespace YangKai.BlogEngine.Web.Mvc.Controllers
{
    public class ArticleController : ApiController
    {
        public PageList<Post> Get(int page = 1, string channel = null, string group = null,
                                           string category = null, string tag = null,
                                           string date = null, string search = null)
        {
            var data = Proxy.Repository.Post.GetAll(
                p => p.PostStatus == (int) PostStatusEnum.Publish,
                new OrderByExpression<Post, DateTime>(p => p.CreateDate, OrderMode.DESC));

            data = data.Where(p => p.Group.Channel.Url == channel || string.IsNullOrEmpty(channel))
                       .Where(p => p.Group.Url == group || string.IsNullOrEmpty(group))
                       .Where(p => p.Categorys.Any(c => c.Url == category) || string.IsNullOrEmpty(category))
                       .Where(p => p.Tags.Any(t => t.Name == tag) || string.IsNullOrEmpty(tag))
                       .Where(p => p.Title.Contains(search) || string.IsNullOrEmpty(search));
            var count = data.Count();
            var list = data.Skip((page - 1)*Config.Setting.PAGE_SIZE).Take(Config.Setting.PAGE_SIZE).ToList();

            //保存搜索记录
            if (!string.IsNullOrEmpty(search))
            {
                var log = Log.CreateSearchLog(search);
                Proxy.Repository.Log.Add(log);
            }

            var result = new PageList<Post>(Config.Setting.PAGE_SIZE)
                {
                    DataList = list,
                    TotalCount = count
                };

            //生成Http-head link
//            PageHelper.SetLinkHeader(result, "/api/article", page, new Dictionary<string, object>
//                {
//                    {"channel", channel},
//                    {"group", group},
//                    {"category", category},
//                    {"tag", tag},
//                    {"date", date},
//                    {"search", search},
//                });

            return result;
        }

        public Post Get(string id)
        {
            var data = Proxy.Repository.Post.Get(p=>p.Url==id);

            if (data == null || data.PostStatus == (int) PostStatusEnum.Trash)
            {
                return null;
            }

            data.ReplyCount++;
            Proxy.Repository.Post.Update(data);

            return data;
        }

        public object Get(Guid id,string action)
        {
            if (action == "nav")//上一篇 & 下一篇
            {
                var post = Proxy.Repository.Post.Get(id);
               var prePost = GetPrePost(post)??new Post();
               var nextPost = GetNextPost(post) ?? new Post();

                var list = new List<Post>();
                list.AddRange(new List<Post> { prePost , nextPost});
                return list.Select(p => new { p.Title, p.Url });
            }
            if (action == "related")//相关文章
            {
                var post = Proxy.Repository.Post.Get(id);
                IList<Post> result = new List<Post>();
                if (post.Tags != null)
                {
                    foreach (Tag tag in post.Tags)
                    {
                        Proxy.Repository.Tag.GetAll(p => p.Name == tag.Name)
                                      .Select(p => p.Post).ToList().ForEach(result.Add);
                    }
                }
                return result.Distinct().Where(p => p.PostId != id && p.Group == post.Group).Take(10).ToList();
            }
            return null;
        }

        private Post GetPrePost(Post entity)
        {
            Expression<Func<Post, bool>> specExpr = p => p.PubDate < entity.PubDate
                                                         && p.PostStatus == (int)PostStatusEnum.Publish
                                                         && p.GroupId == entity.GroupId;
            var result = Proxy.Repository.Post.GetAll(1, specExpr,
                                new OrderByExpression<Post, DateTime>(
                                    p => p.PubDate, OrderMode.DESC));
            return result.FirstOrDefault();
        }

        private Post GetNextPost(Post entity)
        {
            Expression<Func<Post, bool>> specExpr =
                p => p.PubDate > entity.PubDate
                     && p.PostStatus == (int)PostStatusEnum.Publish
                     && p.GroupId == entity.GroupId;
            var result = Proxy.Repository.Post.GetAll(1, specExpr,
                                new OrderByExpression<Post, DateTime>(
                                    p => p.PubDate));
            return result.FirstOrDefault();
        }

        //public ActionResult Calendar(string channelUrl)
        //{
        //    if (!string.IsNullOrEmpty(channelUrl))
        //    {
        //        ViewBag.Channel = QueryFactory.Instance.Post.GetChannel(channelUrl);
        //    }
        //    ViewBag.CalendarList = QueryFactory.Instance.Post.GroupByCalendar(channelUrl);
        //    var data = QueryFactory.Instance.Post.FindAll(channelUrl);
        //    return View(data);
        //}

        ////相关文章 || 随便看看  
        //[ActionName("detail-related")]
        //public ActionResult PostRelated(Guid postId)
        //{
        //    var data = QueryFactory.Instance.Post.FindAllByTag(postId, 7);
        //    ViewBag.IsExistPostRelated = true;
        //    if (data.Count == 0)
        //    {
        //        data = QueryFactory.Instance.Post.FindAllByRandom(postId, 7);
        //        ViewBag.IsExistRelatedPosts = false;
        //    }
        //    return View(data);
        //}
    }
}
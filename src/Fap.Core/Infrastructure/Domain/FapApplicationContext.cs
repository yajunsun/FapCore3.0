﻿
using Fap.Core.DataAccess;
using Fap.Core.Infrastructure.Cache;
using Fap.Core.Infrastructure.Enums;
using Fap.Core.Rbac.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Fap.Core.Infrastructure.Domain
{
    public class FapApplicationContext : IFapApplicationContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDbContext _dbContext;
        private readonly ICacheService _cacheService;
        public FapApplicationContext(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, ICacheFactory cacheFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
            _cacheService = cacheFactory.GetCacheService(CacheEnum.Memory);
        }
        /// <summary>
        /// 员工Fid
        /// </summary>
        public string EmpUid => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
        /// <summary>
        /// 员工姓名
        /// </summary>
        public string EmpName => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        /// <summary>
        /// 用户Fid
        /// </summary>
        public string UserUid => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        /// <summary>
        /// 用户类型
        /// </summary>
        public string UserType => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        /// <summary>
        /// 部门Fid
        /// </summary>
        public string DeptUid => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.PrimarySid)?.Value;
        /// <summary>
        /// 部门Code
        /// </summary>
        public string DeptCode => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.PrimaryGroupSid)?.Value;
        /// <summary>
        /// 部门名称
        /// </summary>
        public string DeptName => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.System)?.Value;
        /// <summary>
        /// 组织
        /// </summary>
        public string OrgUid => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.DenyOnlyPrimarySid)?.Value;
        /// <summary>
        /// 集团
        /// </summary>
        public string GroupUid => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.DenyOnlyPrimaryGroupSid)?.Value;
        /// <summary>
        /// 当前语言
        /// </summary>
        public MultiLanguageEnum Language => _httpContextAccessor?.HttpContext == null ? MultiLanguageEnum.ZhCn : (MultiLanguageEnum)Enum.Parse(typeof(MultiLanguageEnum), _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value ?? "ZhCn");
        /// <summary>
        /// 在线用户
        /// </summary>
        public string OnlineUserUid => _httpContextAccessor?.HttpContext == null ? "-" : _httpContextAccessor?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor)?.Value ?? "temp";
        /// <summary>
        /// 所有角色UID
        /// </summary>
        public IEnumerable<string> Roles => _httpContextAccessor?.HttpContext == null ? Array.Empty<string>() : _httpContextAccessor?.HttpContext.User.FindAll(c => c.Type == ClaimTypes.Role)?.Select(r => r.Value);

        public FapRole Role { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public FapUser Account
        {
            get
            {
                string key = $"user_{UserUid}";
                var user = _cacheService.Get<FapUser>(key);
                if (user == null)
                {
                    user = _dbContext.Get<FapUser>(UserUid, true);
                    _cacheService.Add(key, user);
                }
                return user;
            }
        }

        public FapOnlineUser OnlineUser => throw new NotImplementedException();

        public Employee Employee
        {
            get
            {
                string key = $"employee_{EmpUid}";
                var employee = _cacheService.Get<Employee>(key);
                if (employee == null)
                {
                    employee = _dbContext.Get<Employee>(EmpUid, true);
                    _cacheService.Add(key, employee);
                }
                return employee;
            }
        }

        public string CurrentRoleUid { get; set; } = PlatformConstants.CommonUserRoleFid;
    }
}

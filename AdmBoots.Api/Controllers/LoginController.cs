﻿using System.Threading.Tasks;
using AdmBoots.Api.Authorization;
using AdmBoots.Application.Users;
using AdmBoots.Application.Users.Dto;
using AdmBoots.Infrastructure.Authorization;
using AdmBoots.Infrastructure.Framework.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace AdmBoots.Api.Controllers {
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    [AllowAnonymous]
    public class LoginController : ControllerBase {
        private readonly IUserService _userService;
        private readonly AdmPolicyRequirement _requirement;
        private readonly IDistributedCache _cache;

        public LoginController(IUserService userService, AdmPolicyRequirement requirement, IDistributedCache cache) {
            _userService = userService;
            _requirement = requirement;
            _cache = cache;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]LoginInput input) {
            if (string.IsNullOrWhiteSpace(input.UserName)
                || string.IsNullOrWhiteSpace(input.Password)) {
                return Ok(ResponseBody.Bad("用户名或密码不能为空"));
            }
            var user = await _userService.LonginAsync(input);
            if (user != null) {
                return Ok(ResponseBody.From(AuthenticateResult.Get(user, _requirement, _cache)));
            }
            return Ok(ResponseBody.Bad("用户名或密码错误"));
        }

        /// <summary>
        /// 请求刷新Token（以旧换新）
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet("refresh-token/{token}")]
        public async Task<object> RefreshToken(string token) {
            if (string.IsNullOrEmpty(token)) {
                return Ok(ResponseBody.Bad("令牌无效，请重新登陆"));
            }
            var uid = JwtToken.ReadJwtToken<int>(token);

            var user = await _userService.GetLoginUserAsync(uid);
            if (user != null) {
                return Ok(ResponseBody.From(AuthenticateResult.Get(user, _requirement, _cache)));
            }
            return Ok(ResponseBody.Bad("认证失败"));
        }
    }
}

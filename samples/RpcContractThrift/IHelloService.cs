
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcContractThrift
{
    public interface IHelloService
    {
        string CallNameVoid();
        void CallName(string name);
        void CallVoid();
        string Hello(string name);
        HelloResult SayHello(string name);
        string ShowHello(HelloResult name);
        /// <summary>
        /// 用户LocalSession信息获取并将相关公共内容同步memcache缓存
        /// </summary>
        /// <param name="sessionId"></param>
        string GetLocalSession(long sessionId, string time);
        /// <summary>
        /// 用户RoleSession信息获取并将相关公共内容同步memcache缓存
        /// </summary>
        /// <param name="keys"></param>
        Dictionary<long, string> GetRoleSession(List<long> keys);
        /// <summary>
        /// 获取用户token
        /// </summary>
        /// <param name="id"></param>
        /// <param name="openType"></param>
        /// <returns></returns>
        string GetAuthToken(long id, int openType);
        /// <summary>
        /// 更新对应角色用户的会话数据
        /// </summary>
        /// <param name="roles"></param>
        /// <param name="updater"></param>
        bool UpdateRole(List<long> roles, string updater);
        /// <summary>
        /// 更新对应菜单用户的会话数据
        /// </summary>
        /// <param name="menus"></param>
        /// <param name="updater"></param>
        bool UpdateMenu(List<long> menus, string updater);
        /// <summary>
        /// 更新用户详细信息
        /// </summary>
        /// <param name="detail"></param>
        bool UpdateUserDetail(string detail);
        /// <summary>
        /// 更新用户头像信息
        /// </summary>
        /// <param name="detail"></param>
        bool UpdateUserHead(string head);
        /// <summary>
        /// 更新用户动态数据
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="dataType"></param>
        /// <param name="content"></param>
        bool UpdateDynamic(long userId, string dataType, string content);
        /// <summary>
        /// 刷新会话
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        bool RefreshSession(long userId);
        /// <summary>
        /// 用户冻结
        /// </summary>
        /// <param name="username"></param>
        /// <param name="frozenTime"></param>
        bool MemberFrozen(string username, string frozenTime);
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="pwd"></param>
        /// <param name="rememberType"></param>
        /// <param name="sessionEntity"></param>
        string MemberLogin(string userName, string pwd, int rememberType, string sessionEntity);
        /// <summary>
        /// 登录 证件登录
        /// </summary>
        /// <param name="codeType"></param>
        /// <param name="codeNum"></param>
        /// <param name="pwd"></param>
        /// <param name="rememberType"></param>
        /// <param name="sessionEntity"></param>
        string MemberCodeLogin(int codeType, string codeNum, string pwd, int rememberType, string sessionEntity);
        /// <summary>
        /// 登录 第三方登录
        /// </summary>
        /// <param name="thirdType"></param>
        /// <param name="openId"></param>
        /// <param name="sessionEntity"></param>
        /// <returns></returns>
        string MemberThirdLogin(string token, string sessionEntity);
        /// <summary>
        /// 登录 短信码登录
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="smsCode"></param>
        /// <param name="sessionEntity"></param>
        /// <returns></returns>
        string MemberSmsLogin(string mobile, string smsCode, string sessionEntity);
        /// <summary>
        /// 登录 二维码扫码登录
        /// </summary>
        /// <param name="qrCode"></param>
        /// <param name="sessionEntity"></param>
        /// <returns></returns>
        string MemberQRCodeLogin(string qrCode, string sessionEntity);
        /// <summary>
        /// 自动登录 并同步到异步缓存中去2: string sessionId,
        /// </summary>
        /// <param name="cookieSession"></param>
        /// <param name="userHostAddress"></param>
        string MemberAutoLogin(string cookieSession, string userHostAddress);
        /// <summary>
        /// 用户锁定
        /// </summary>
        /// <param name="cookieSession"></param>
        bool MemberLock(string cookieSession);
        /// <summary>
        /// 用户解除锁定
        /// </summary>
        /// <param name="cookieSession"></param>
        /// <param name="pwd"></param>
        bool MemberUnLock(string cookieSession, string pwd);
        /// <summary>
        /// 登出
        /// </summary>
        /// <param name="cookieSession"></param>
        /// <param name="status"></param>
        bool MemberLogOut(string cookieSession, int status);
        /// <summary>
        /// 获得所有用户信息
        /// </summary>
        List<string> GetUserChat();
        /// <summary>
        /// 获得用户Open集合
        /// </summary>
        /// <param name="ids">dtIds</param>
        /// <param name="openType">第三方类型</param>
        /// <returns></returns>
        List<string> GetOpenList(List<long> ids, int openType);

        /// <summary>
        /// 仓储所有失效的会话
        /// </summary>
        /// <returns></returns>
        bool SessionStoreAll();
        /// <summary>
        /// 仓储所有选中的会话
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        bool SessionStoreSelected(List<long> ids);
    }
}

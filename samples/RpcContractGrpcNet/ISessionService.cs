using Zooyard.Attributes;

namespace MemberGrpc;

[ZooyardGrpcNet("SessionService", typeof(MemberGrpc.SessionService.SessionServiceClient), Url = "http://localhost:6662?cluster=failfast&timeout=5000")]
public interface ISessionService
{
    /// <summary>
    /// 创建用于应用访问的AccessToken
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberRefresh(MemberRefreshRequest req);
    /// <summary>
    /// 用户冻结
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberFrozen(MemberFrozenRequest req);
    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberLogin(MemberLoginRequest req);
    /// <summary>
    /// 登录 第三方OpenId登录
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberThirdLogin(MemberThirdLoginRequest req);

    /// <summary>
    /// 登录 短信码登录
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberSmsLogin(MemberSmsLoginRequest req);
    /// <summary>
    /// 登录 二维码扫码登录
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberQRCodeLogin(MemberQRCodeLoginRequest req);
    /// <summary>
    /// 锁定
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberLock(MemberLockRequest req);
    /// <summary>
    /// 用户解除锁定
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberUnLock(MemberUnLockRequest req);
    /// <summary>
    /// 登出
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> MemberLogOut(MemberLogOutRequest req);
    /// <summary>
    /// 获取接口内容
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> GetApi(GetApiRequest req);

    /// <summary>
    /// User Touch api get user message
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> UserTouch(TouchRequest req);

    /// <summary>
    /// app Touch api get user message
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> AppTouch(TouchRequest req);

    /// <summary>
    /// Basic Touch api get user message
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    Task<ResponseMessage> BasicTouch(BasicTouchRequest req);
    Task<ResponseMessage> GetUsersByArrayId(IEnumerable<long> ids);
    Task<ResponseMessage> GetUser(long id);
    Task<ResponseMessage> GetUsersByRoleId(IEnumerable<long> id);
    Task<ResponseMessage> GetUserByDepartmentIds(IEnumerable<long> id);
    
}


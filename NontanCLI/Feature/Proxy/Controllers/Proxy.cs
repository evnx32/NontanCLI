﻿using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using M3U8Proxy.RequestHandler;
using M3U8Proxy.RequestHandler.AfterReceive;
using M3U8Proxy.RequestHandler.BeforeSend;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace M3U8Proxy.Controllers;

[EnableCors("corsPolicy")]
[ApiController]
[Route("[controller]")]
public partial class Proxy : Controller
{
    private readonly ReqHandler _reqHandler = new();

    [HttpHead]
    [HttpGet]
    [Route("{url}/{headers?}/{type?}")]
    public Task GetProxy(string url, string? headers = "{}", string? forcedHeadersProxy = "{}")
    {
        try
        {
            url = Uri.UnescapeDataString(url);
            headers = Uri.UnescapeDataString(headers!);
            forcedHeadersProxy = Uri.UnescapeDataString(forcedHeadersProxy!);

            var forcedHeadersProxyDictionary =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(forcedHeadersProxy);
            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);

            var options = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
                .WithBeforeSend((_, hrm) =>
                {
                    if (headersDictionary == null) return Task.CompletedTask;
                    BeforeSend.RemoveHeaders(hrm);
                    BeforeSend.AddHeaders(headersDictionary, hrm); 
                    return Task.CompletedTask;
                })
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(e));
                })
                .WithAfterReceive((_, hrm) =>
                {
                    AfterReceive.RemoveHeaders(hrm);
                    AfterReceive.AddForcedHeaders(forcedHeadersProxyDictionary, hrm);
                    return Task.CompletedTask;
                })
                .Build();
            return this.HttpProxyAsync(url, options);
        }
        catch (Exception e)
        {
            HandleExceptionResponse(e);
            return Task.FromResult(0);
        }
    }

    

    private void HandleExceptionResponse(Exception e)
    {
        HttpContext.Response.StatusCode = 400;
        HttpContext.Response.ContentType = "application/json";
        HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(e));
    }

   

}
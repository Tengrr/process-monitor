﻿using ProcessMonitor.WebApi.Models;
using ProcessMonitor.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using Microsoft.Graph;
using System.Web;
using Azure.Identity;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.DirectoryServices.AccountManagement;

namespace ProcessMonitor.WebApi.Controllers;

public class UserIdentity
{
    public string? user_principal_name { get; set; }

}

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class OperationsController : ControllerBase
{

    private readonly OperationsService _operationsService;
    private string groupid = "7a4b06e4-7e5c-42ad-b122-a0c2a0c116de";

    public OperationsController(OperationsService operationsService)
    {
        _operationsService = operationsService;
    }

    [HttpGet]
    public async Task<List<Models.Operation>> Get() =>
        await _operationsService.GetAsync();


    /// <summary>
    /// Get operations
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Models.Operation>> Get(string id)
    {
        string authHeader = HttpContext.Request.Headers["Authorization"];
        if (authHeader != null && authHeader.StartsWith("Bearer"))
        {
            string token = authHeader.Substring("Bearer ".Length).Trim();
            string tokenJwt = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            string payload = tokenJwt.Split('}')[1]+"}";
            UserIdentity userIdentity = JsonConvert.DeserializeObject<UserIdentity>(payload);
            PrincipalContext pc = new PrincipalContext(ContextType.Domain);

            var user = UserPrincipal.FindByIdentity(pc,userIdentity.user_principal_name);
            var groupPrincipal = GroupPrincipal.FindByIdentity(pc,groupid);

            if (user.IsMemberOf(groupPrincipal))
            {
                var operation = await _operationsService.GetAsync(id);
                return operation;
            }
            else
            {
                return NotFound();
            }

        }
        else
        {
            //Handle what happens if that isn't the case
            throw new Exception("The authorization header is either empty or isn't Basic.");
        }

        
    }
    /// <summary>
    /// Put operations
    /// </summary>
    /// <param name="newOperation"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Post(Models.Operation newOperation)
    {
        string authHeader = HttpContext.Request.Headers["Authorization"];
        if (authHeader != null && authHeader.StartsWith("Bearer"))
        {
            string token = authHeader.Substring("Bearer ".Length).Trim();
            string tokenJwt = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            string payload = tokenJwt.Split('}')[1] + "}";
            UserIdentity userIdentity = JsonConvert.DeserializeObject<UserIdentity>(payload);
            PrincipalContext pc = new PrincipalContext(ContextType.Domain);

            var user = UserPrincipal.FindByIdentity(pc, userIdentity.user_principal_name);
            var groupPrincipal = GroupPrincipal.FindByIdentity(pc, groupid);

            if (user.IsMemberOf(groupPrincipal))
            {
                // Store the operation into db
                await _operationsService.CreateAsync(newOperation);

                // Send the operation to service bus
                await MessageSender.SendOperationToQueue(newOperation);

                return CreatedAtAction(nameof(Get), new { newOperation.id }, newOperation);
            }
            else
            {
                return CreatedAtAction("failed", new { newOperation.id }, newOperation);
            }

        }
        else
        {
            //Handle what happens if that isn't the case
            throw new Exception("The authorization header is either empty or isn't Basic.");
        }


   
    }

}
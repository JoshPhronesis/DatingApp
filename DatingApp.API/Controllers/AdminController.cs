using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public AdminController(DataContext context, UserManager<User> userManager, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            this._cloudinaryConfig = cloudinaryConfig;
            this._context = context;
            this._userManager = userManager;

            Account acct = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acct);
        }

        [HttpGet("usersWithRoles")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var userList = await (from user in _context.Users
                                  orderby user.UserName
                                  select new
                                  {
                                      Id = user.Id,
                                      UserName = user.UserName,
                                      Roles = (from userRole in user.UserRoles
                                               join role in _context.Roles
                                               on userRole.RoleId
                                               equals role.Id
                                               select role.Name).ToList()
                                  }
                            ).ToListAsync();

            return Ok(userList);
        }

        [HttpGet("photosForModeration")]
        [Authorize(Policy = "ModeratePhotoRole")]
        public async Task<IActionResult> GetPhotosForModeration()
        {
            var photos = await _context.Photos
                                .Include(u => u.User)
                                .IgnoreQueryFilters()
                                .Where(p => p.IsApproved == false)
                                .Select(u => new {
                                    Id = u.Id,
                                    UserName = u.User.UserName,
                                    Url = u.Url,
                                    IsApproved = u.IsApproved
                                }).ToListAsync();

            return Ok(photos);
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approvePhoto/{photoId}")]
        public async Task<IActionResult> ApprovePhoto(int photoId) {
            var photo = await _context.Photos.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(p => p.Id == photoId);

            photo.IsApproved =true;
            await _context.SaveChangesAsync();

            return Ok();
        }

         [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("rejectPhoto/{photoId}")]
        public async Task<IActionResult> RejectPhoto(int photoId) {
            var photo = await _context.Photos.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(p => p.Id == photoId);

            if(photo.IsMain){
                return BadRequest("Unable to reject main photo");
            }

            if(photo.PublicId != null) {
                var deleteParams = new DeletionParams(photo.PublicId);
                var result = _cloudinary.Destroy(deleteParams);

                if(result.Result == "ok"){
                    _context.Photos.Remove(photo);
                }
            } 
            else {
                _context.Photos.Remove(photo);
            }
            
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("editRoles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
        {
            var user = await _userManager.FindByNameAsync(userName);
            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = roleEditDto.RoleNames;
            selectedRoles = selectedRoles ?? new string[] { };
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded)
            {
                return BadRequest("Failed to add to roles");
            }

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded)
            {
                return BadRequest("Failed to remove the roles");
            }

            return Ok(await _userManager.GetRolesAsync(user));
        }

    }
}
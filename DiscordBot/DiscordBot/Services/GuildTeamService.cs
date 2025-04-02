using DiscordBot.Data;
using DiscordBot.Dtos;
using DiscordBot.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Services
{
    public class GuildTeamService
    {
        private readonly ApplicationDbContext _dbContext;

        public GuildTeamService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<TeamGroup>> GetFullTeamStructureAsync()
        {
            return await _dbContext.TeamGroups
            .Include(g => g.GuildTeams.OrderBy(t => t.Order))
            .ThenInclude(t => t.TeamMembers)
            .ToListAsync();
        }

        public async Task SaveTeamStructureAsync(List<GuildTeam> updatedTeams)
        {
            // 將前端傳來的所有成員資料整理成一包
            var incomingMembers = updatedTeams
                .SelectMany(t => t.TeamMembers.Select(m => new
                {
                    MemberId = m.Id, // 這是 GuildTeamMember.Id（int，不是 null）
                    TeamId = t.Id,
                    Position = m.Position
                }))
                .ToList();

            // 從資料庫取出這些 member 的資料
            var memberIds = incomingMembers.Select(m => m.MemberId).ToList();

            var dbMembers = await _dbContext.GuildTeamMembers
                .Where(m => memberIds.Contains(m.Id))
                .ToListAsync();

            // 針對每位成員進行更新
            foreach (var incoming in incomingMembers)
            {
                var dbMember = dbMembers.FirstOrDefault(m => m.Id == incoming.MemberId);
                if (dbMember != null)
                {
                    bool updated = false;

                    if (dbMember.GuildTeamId != incoming.TeamId)
                    {
                        dbMember.GuildTeamId = incoming.TeamId;
                        updated = true;
                    }

                    if (dbMember.Position != incoming.Position)
                    {
                        dbMember.Position = incoming.Position;
                        updated = true;
                    }

                    if (updated)
                    {
                        _dbContext.GuildTeamMembers.Update(dbMember);
                    }
                }
            }

            // 更新隊伍名稱
            var teamIds = updatedTeams.Select(t => t.Id).ToList();
            var dbTeams = await _dbContext.GuildTeams
                .Where(t => teamIds.Contains(t.Id))
                .ToListAsync();

            foreach (var updatedTeam in updatedTeams)
            {
                var dbTeam = dbTeams.FirstOrDefault(t => t.Id == updatedTeam.Id);
                if (dbTeam != null && dbTeam.TeamName != updatedTeam.TeamName)
                {
                    dbTeam.TeamName = updatedTeam.TeamName;
                    _dbContext.GuildTeams.Update(dbTeam);
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}

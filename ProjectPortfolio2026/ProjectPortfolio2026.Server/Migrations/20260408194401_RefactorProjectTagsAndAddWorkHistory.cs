using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPortfolio2026.Server.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProjectTagsAndAddWorkHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StreetAddress1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StreetAddress2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployerId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SupervisorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescriptionMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobRoles_Employers_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "Employers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTags",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTags", x => new { x.ProjectId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ProjectTags_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobRoleTags",
                columns: table => new
                {
                    JobRoleId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRoleTags", x => new { x.JobRoleId, x.TagId });
                    table.ForeignKey(
                        name: "FK_JobRoleTags_JobRoles_JobRoleId",
                        column: x => x.JobRoleId,
                        principalTable: "JobRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobRoleTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobRoles_EmployerId",
                table: "JobRoles",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRoleTags_TagId",
                table: "JobRoleTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTags_TagId",
                table: "ProjectTags",
                column: "TagId");

            migrationBuilder.Sql(
                """
                INSERT INTO Tags (Category, DisplayName, NormalizedName)
                SELECT grouped.Category, grouped.DisplayName, grouped.NormalizedName
                FROM
                (
                    SELECT
                        1 AS Category,
                        MIN(LTRIM(RTRIM(Name))) AS DisplayName,
                        UPPER(LTRIM(RTRIM(Name))) AS NormalizedName
                    FROM ProjectSkills
                    GROUP BY UPPER(LTRIM(RTRIM(Name)))

                    UNION ALL

                    SELECT
                        2 AS Category,
                        MIN(LTRIM(RTRIM(Name))) AS DisplayName,
                        UPPER(LTRIM(RTRIM(Name))) AS NormalizedName
                    FROM ProjectTechnologies
                    GROUP BY UPPER(LTRIM(RTRIM(Name)))
                ) grouped
                WHERE grouped.NormalizedName <> '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO ProjectTags (ProjectId, TagId)
                SELECT DISTINCT projectSkills.ProjectId, tags.Id
                FROM ProjectSkills projectSkills
                INNER JOIN Tags tags
                    ON tags.Category = 1
                    AND tags.NormalizedName = UPPER(LTRIM(RTRIM(projectSkills.Name)));

                INSERT INTO ProjectTags (ProjectId, TagId)
                SELECT DISTINCT projectTechnologies.ProjectId, tags.Id
                FROM ProjectTechnologies projectTechnologies
                INNER JOIN Tags tags
                    ON tags.Category = 2
                    AND tags.NormalizedName = UPPER(LTRIM(RTRIM(projectTechnologies.Name)));
                """);

            migrationBuilder.Sql(
                """
                ;WITH DuplicateTags AS
                (
                    SELECT
                        tags.Id,
                        tags.Category,
                        tags.NormalizedName,
                        MIN(tags.Id) OVER (PARTITION BY tags.Category, tags.NormalizedName) AS CanonicalTagId
                    FROM Tags tags
                )
                UPDATE projectTags
                SET TagId = duplicateTags.CanonicalTagId
                FROM ProjectTags projectTags
                INNER JOIN DuplicateTags duplicateTags ON duplicateTags.Id = projectTags.TagId
                WHERE duplicateTags.Id <> duplicateTags.CanonicalTagId
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM ProjectTags existingProjectTag
                      WHERE existingProjectTag.ProjectId = projectTags.ProjectId
                        AND existingProjectTag.TagId = duplicateTags.CanonicalTagId
                  );

                ;WITH DuplicateTags AS
                (
                    SELECT
                        tags.Id,
                        tags.Category,
                        tags.NormalizedName,
                        MIN(tags.Id) OVER (PARTITION BY tags.Category, tags.NormalizedName) AS CanonicalTagId
                    FROM Tags tags
                )
                DELETE projectTags
                FROM ProjectTags projectTags
                INNER JOIN DuplicateTags duplicateTags ON duplicateTags.Id = projectTags.TagId
                WHERE duplicateTags.Id <> duplicateTags.CanonicalTagId;

                ;WITH DuplicateTags AS
                (
                    SELECT
                        tags.Id,
                        tags.Category,
                        tags.NormalizedName,
                        MIN(tags.Id) OVER (PARTITION BY tags.Category, tags.NormalizedName) AS CanonicalTagId
                    FROM Tags tags
                )
                DELETE jobRoleTags
                FROM JobRoleTags jobRoleTags
                INNER JOIN DuplicateTags duplicateTags ON duplicateTags.Id = jobRoleTags.TagId
                WHERE duplicateTags.Id <> duplicateTags.CanonicalTagId
                  AND EXISTS
                  (
                      SELECT 1
                      FROM JobRoleTags existingJobRoleTag
                      WHERE existingJobRoleTag.JobRoleId = jobRoleTags.JobRoleId
                        AND existingJobRoleTag.TagId = duplicateTags.CanonicalTagId
                  );

                ;WITH DuplicateTags AS
                (
                    SELECT
                        tags.Id,
                        tags.Category,
                        tags.NormalizedName,
                        MIN(tags.Id) OVER (PARTITION BY tags.Category, tags.NormalizedName) AS CanonicalTagId
                    FROM Tags tags
                )
                UPDATE jobRoleTags
                SET TagId = duplicateTags.CanonicalTagId
                FROM JobRoleTags jobRoleTags
                INNER JOIN DuplicateTags duplicateTags ON duplicateTags.Id = jobRoleTags.TagId
                WHERE duplicateTags.Id <> duplicateTags.CanonicalTagId;

                ;WITH DuplicateTags AS
                (
                    SELECT
                        tags.Id,
                        tags.Category,
                        tags.NormalizedName,
                        MIN(tags.Id) OVER (PARTITION BY tags.Category, tags.NormalizedName) AS CanonicalTagId
                    FROM Tags tags
                )
                DELETE FROM Tags
                WHERE Id IN
                (
                    SELECT duplicateTags.Id
                    FROM DuplicateTags duplicateTags
                    WHERE duplicateTags.Id <> duplicateTags.CanonicalTagId
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Category_NormalizedName",
                table: "Tags",
                columns: new[] { "Category", "NormalizedName" },
                unique: true);

            migrationBuilder.DropTable(
                name: "ProjectSkills");

            migrationBuilder.DropTable(
                name: "ProjectTechnologies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectSkills_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTechnologies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTechnologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTechnologies_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSkills_ProjectId",
                table: "ProjectSkills",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTechnologies_ProjectId",
                table: "ProjectTechnologies",
                column: "ProjectId");

            migrationBuilder.Sql(
                """
                INSERT INTO ProjectSkills (ProjectId, Name)
                SELECT DISTINCT projectTags.ProjectId, tags.DisplayName
                FROM ProjectTags projectTags
                INNER JOIN Tags tags ON tags.Id = projectTags.TagId
                WHERE tags.Category = 1;

                INSERT INTO ProjectTechnologies (ProjectId, Name)
                SELECT DISTINCT projectTags.ProjectId, tags.DisplayName
                FROM ProjectTags projectTags
                INNER JOIN Tags tags ON tags.Id = projectTags.TagId
                WHERE tags.Category = 2;
                """);

            migrationBuilder.DropTable(
                name: "JobRoleTags");

            migrationBuilder.DropTable(
                name: "ProjectTags");

            migrationBuilder.DropTable(
                name: "JobRoles");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Employers");
        }
    }
}

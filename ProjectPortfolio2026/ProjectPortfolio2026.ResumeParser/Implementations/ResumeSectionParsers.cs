using ProjectPortfolio2026.ResumeParser.Interfaces;
using ProjectPortfolio2026.ResumeParser.Models;

namespace ProjectPortfolio2026.ResumeParser.Implementations;

internal sealed class HeaderResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "__header__";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.Header = ResumeParsingUtilities.ParseHeader(lines);
}

internal sealed class ProfessionalSummaryResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "summary";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.ProfessionalSummary = ResumeParsingUtilities.JoinSectionParagraph(lines);
}

internal sealed class WorkExperienceResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "experience";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.WorkExperience = ResumeParsingUtilities.ParseWorkExperience(lines);
}

internal sealed class EducationResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "education";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.Education = ResumeParsingUtilities.ParseEducation(lines);
}

internal sealed class SkillsResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "skills";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.Skills = ResumeParsingUtilities.ParseSkillSections(lines);
}

internal sealed class CertificationsResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "certifications";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.Certifications = ResumeParsingUtilities.ParseCertifications(lines);
}

internal sealed class ProjectsResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "projects";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.Projects = ResumeParsingUtilities.ParseProjects(lines);
}

internal sealed class LanguagesResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "languages";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.Languages = ResumeParsingUtilities.ParseLanguages(lines);
}

internal sealed class AwardsResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "awards";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.Awards = ResumeParsingUtilities.ParseAwards(lines);
}

internal sealed class VolunteerExperienceResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "volunteer";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.VolunteerExperience = ResumeParsingUtilities.ParseVolunteerExperience(lines);
}

internal sealed class PublicationsResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "publications";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.Publications = ResumeParsingUtilities.ParsePublications(lines);
}

internal sealed class ReferencesResumeSectionParser : IResumeSectionParser
{
    public string SectionKey => "references";
    public void Parse(IReadOnlyList<string>? lines, ResumeDocumentBuilder builder) => builder.References = ResumeParsingUtilities.ParseReferences(lines);
}

internal sealed class AdditionalResumeSectionParser : IResumeAdditionalSectionParser
{
    public List<ResumeCustomSection> Parse(IReadOnlyDictionary<string, List<string>> sections) => ResumeParsingUtilities.ParseAdditionalSections(sections);
}

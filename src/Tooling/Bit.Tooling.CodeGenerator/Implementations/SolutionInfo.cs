using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bit.Tooling.CodeGenerator.Implementations
{
    // https://stackoverflow.com/a/26129175/2720104
    public class ProjectInfo
    {
        public string? ParentProjectGuid { get; set; }
        public string? ProjectName { get; set; }
        public string? RelativePath { get; set; }
        public string? AbsolutePath { get; set; }
        public string? ProjectGuid { get; set; }
    }

    public class SolutionInfo
    {
        private readonly List<object> _slnLines; // List of either String (line format is not interesting to us), or SolutionProject.
        private string _solutionFileName;

        public SolutionInfo(string solutionFileName)
        {
            _solutionFileName = solutionFileName;
            _slnLines = new List<object>();
            string slnTxt = File.ReadAllText(_solutionFileName);
            string[] lines = slnTxt.Split('\n');
            Regex projMatcher = new Regex("Project\\(\"(?<ParentProjectGuid>{[A-F0-9-]+})\"\\) = \"(?<ProjectName>.*?)\", \"(?<RelativePath>.*?)\", \"(?<ProjectGuid>{[A-F0-9-]+})");

            Regex.Replace(slnTxt, "^(.*?)[\n\r]*$", m =>
            {
                string line = m.Groups[1].Value;

                Match m2 = projMatcher.Match(line);
                if (m2.Groups.Count < 2)
                {
                    _slnLines.Add(line);
                    return "";
                }

                ProjectInfo s = new ProjectInfo();
                foreach (string g in projMatcher.GetGroupNames().Where(x => x != "0")) /* "0" - RegEx special kind of group */
                    s.GetType().GetProperty(g).SetValue(s, m2.Groups[g].ToString());

                _slnLines.Add(s);
                return "";
            },
            RegexOptions.Multiline
            );
        }

        public List<ProjectInfo> GetProjects(bool bGetAlsoFolders = false)
        {
            var q = _slnLines.OfType<ProjectInfo>();

            if (!bGetAlsoFolders)  // Filter away folder names in solution.
                q = q.Where(x => x.RelativePath != x.ProjectName);

            var result = q.ToList();

            foreach (var p in result)
            {
                p.AbsolutePath = Path.Combine(Path.GetDirectoryName(_solutionFileName), p.RelativePath);
            }

            return result;
        }
    }
}

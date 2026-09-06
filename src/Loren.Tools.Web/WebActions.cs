using Loren.Core.Actions;

namespace Loren.Tools.Web;

public static class WebActions
{
    public static readonly ActionDefinition Search = new(
        "web.search",
        "Search the current public web for information that may have changed or requires fresh verification. Use this for current facts, recent developments, latest versions, current availability, or when the owner explicitly asks to search the web. Ground the final answer in returned sources and include source URLs.",
        true,
        [
            new ActionParameterDefinition(
                "query",
                "A concise web search query.",
                ActionParameterType.Text,
                true),
        ]);
}

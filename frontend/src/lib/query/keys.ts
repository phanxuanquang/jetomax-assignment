export const queryKeys = {
  // The list endpoint (GET /api/conversations?q=), one cache entry per filter string.
  conversationList: (filter: string) => ["conversationList", filter] as const,
  conversationListAll: ["conversationList"] as const,
  // Per-conversation sub-resources — a distinct root from conversationList so
  // realtime cache patches never accidentally touch the wrong query's data.
  messages: (conversationId: string) => ["conversation", conversationId, "messages"] as const,
  messageSearch: (conversationId: string, keyword: string) =>
    ["conversation", conversationId, "search", keyword] as const,
  user: (idOrUsername: string) => ["user", idOrUsername] as const,
};

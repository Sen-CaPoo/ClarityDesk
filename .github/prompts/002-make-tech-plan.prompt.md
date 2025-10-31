---
agent: agent
---
/speckit.plan
現有的套件都不要更動，請依據 spec.md 規劃出新的技術方案。

我要整合 LINE Messaging API 實現雙向通訊機制(Push Message),LINE 發送訊息採用 Flex Message 格式提供良好的視覺呈現。

不使用 AutoMapper to map DTO，而是使用 POCO instead。
不使用 Redis。
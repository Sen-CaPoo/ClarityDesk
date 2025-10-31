# Tasks: LINE 整合功能

**Input**: Design documents from `/specs/001-line-integration/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Tests are NOT explicitly requested in the feature specification, so test tasks are marked as OPTIONAL.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Repository root structure for ASP.NET Core Razor Pages application:
- `Models/` - Entities, DTOs, Enums
- `Data/` - DbContext, Configurations, Migrations
- `Services/` - Business logic services
- `Controllers/` - API Controllers
- `Pages/` - Razor Pages
- `Infrastructure/` - Cross-cutting concerns
- `wwwroot/` - Static files
- `Tests/ClarityDesk.UnitTests/` - Unit tests
- `Tests/ClarityDesk.IntegrationTests/` - Integration tests

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and configuration for LINE Messaging API integration

- [ ] T001 Add LINE Messaging configuration section to `appsettings.json` (ChannelAccessToken, ChannelSecret, WebhookPath, ImageUploadPath, MaxImageSizeBytes, MaxImagesPerIssue)
- [ ] T002 [P] Create image upload directory structure `wwwroot/uploads/line-images/`
- [ ] T003 [P] Configure FormOptions in `Program.cs` to allow 30MB multipart body length (3 images × 10MB)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T004 Create `LineBinding` entity in `Models/Entities/LineBinding.cs`
- [ ] T005 [P] Create `LinePushLog` entity in `Models/Entities/LinePushLog.cs`
- [ ] T006 [P] Create `LineConversationState` entity in `Models/Entities/LineConversationState.cs`
- [ ] T007 Create `LinePushStatus` enum in `Models/Enums/LinePushStatus.cs`
- [ ] T008 [P] Create `ConversationStep` enum in `Models/Enums/ConversationStep.cs`
- [ ] T009 Create `LineBindingConfiguration` Fluent API configuration in `Data/Configurations/LineBindingConfiguration.cs`
- [ ] T010 [P] Create `LinePushLogConfiguration` Fluent API configuration in `Data/Configurations/LinePushLogConfiguration.cs`
- [ ] T011 [P] Create `LineConversationStateConfiguration` Fluent API configuration in `Data/Configurations/LineConversationStateConfiguration.cs`
- [ ] T012 Update `ApplicationDbContext.cs` to add DbSet properties for LineBinding, LinePushLog, LineConversationState
- [ ] T013 Create database migration for LINE tables using `dotnet ef migrations add AddLineTables`
- [ ] T014 Create `LineBindingDto` in `Models/DTOs/LineBindingDto.cs`
- [ ] T015 [P] Create `LinePushLogDto` in `Models/DTOs/LinePushLogDto.cs`
- [ ] T016 [P] Create `LineConversationStateDto` in `Models/DTOs/LineConversationStateDto.cs`
- [ ] T017 [P] Create `LineMessageDto` in `Models/DTOs/LineMessageDto.cs` for Webhook event parsing
- [ ] T018 [P] Create `FlexMessageDto` in `Models/DTOs/FlexMessageDto.cs` for Flex Message structure
- [ ] T019 Create `ILineMessagingService` interface in `Services/Interfaces/ILineMessagingService.cs` with methods: PushMessageAsync, PushNewIssueNotificationAsync, PushStatusChangedNotificationAsync, PushAssignmentChangedNotificationAsync, HandleWebhookEventAsync
- [ ] T020 Register HttpClient for LINE Messaging API in `Program.cs` with base address `https://api.line.me/v2/bot/`
- [ ] T021 Register `ILineMessagingService` as scoped service in `Program.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - LINE 官方帳號綁定管理 (Priority: P1) 🎯 MVP

**Goal**: 使用者可透過 LINE Login OAuth 流程綁定/解除綁定 LINE 官方帳號，建立系統帳號與 LINE 帳號的關聯，並在介面中查看綁定狀態

**Independent Test**: 使用者登入後導航至 `/Account/LineBinding` 頁面，點擊「綁定 LINE 官方帳號」按鈕，完成 LINE Login OAuth 流程後，頁面顯示「已綁定」狀態和 LINE 顯示名稱；點擊「解除綁定」後狀態更新為「未綁定」

### Tests for User Story 1 (OPTIONAL) ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T022 [P] [US1] Create `LineBindingPageTests.cs` unit test in `Tests/ClarityDesk.UnitTests/` to test LineBindingPageModel business logic
- [ ] T023 [P] [US1] Create `LineBindingIntegrationTests.cs` in `Tests/ClarityDesk.IntegrationTests/` to test OAuth flow end-to-end

### Implementation for User Story 1

- [ ] T024 [P] [US1] Create `Pages/Account/LineBinding.cshtml` Razor Page view with binding status display, bind/unbind buttons
- [ ] T025 [US1] Create `Pages/Account/LineBinding.cshtml.cs` PageModel with methods: OnGetAsync (load binding status), OnPostBindAsync (initiate LINE Login OAuth), OnGetCallbackAsync (handle OAuth callback), OnPostUnbindAsync (remove binding)
- [ ] T026 [US1] Implement LINE Login OAuth flow in LineBindingPageModel - redirect to LINE authorization endpoint with client_id, redirect_uri, state
- [ ] T027 [US1] Implement OAuth callback handler in LineBindingPageModel - exchange authorization code for access token, retrieve LINE user profile, create/update LineBinding entity
- [ ] T028 [US1] Implement validation logic to prevent duplicate LINE account binding (check existing LineUserId before creating binding)
- [ ] T029 [US1] Implement guest account binding restriction in OnPostBindAsync (check User.Role, return error if guest)
- [ ] T030 [US1] Add error handling for OAuth failures (invalid state, user denied authorization, token exchange failure)
- [ ] T031 [US1] Add logging for binding/unbinding operations using ILogger<LineBindingPageModel>

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - 新增問題時自動推送 LINE 通知 (Priority: P2)

**Goal**: 當系統新增問題回報單或發生關鍵更新時，自動推送 LINE Flex Message 通知給已綁定的相關人員（指派處理人員、原回報人）

**Independent Test**: 在網頁系統新增一筆問題回報單並指派給已綁定 LINE 的處理人員，處理人員的 LINE 應收到包含問題詳細資訊（編號、標題、緊急程度、單位、聯絡人、電話、日期、回報人）和「查看詳細內容」按鈕的 Flex Message；變更狀態或指派人員後，相關人員也應收到通知

### Tests for User Story 2 (OPTIONAL) ⚠️

- [ ] T032 [P] [US2] Create `LineMessagingServiceTests.cs` unit test in `Tests/ClarityDesk.UnitTests/` to test push notification logic with mocked HttpClient
- [ ] T033 [P] [US2] Create `LinePushNotificationIntegrationTests.cs` in `Tests/ClarityDesk.IntegrationTests/` to test end-to-end push flow

### Implementation for User Story 2

- [ ] T034 [P] [US2] Implement `LineMessagingService.cs` with HttpClient injection, configuration binding
- [ ] T035 [US2] Implement `PushMessageAsync` method in LineMessagingService - send POST request to `https://api.line.me/v2/bot/message/push` with Authorization header
- [ ] T036 [US2] Implement `BuildNewIssueFlexMessage` private method in LineMessagingService - construct Flex Message JSON using template from `contracts/flex-message-templates.json` (newIssueNotification)
- [ ] T037 [US2] Implement `BuildStatusChangedMessage` private method in LineMessagingService - construct text message for status updates
- [ ] T038 [US2] Implement `BuildAssignmentChangedFlexMessage` private method in LineMessagingService - construct Flex Message using assignmentChangedNotification template
- [ ] T039 [US2] Implement `PushNewIssueNotificationAsync` method - check if assigned user has active LineBinding, build Flex Message, call PushMessageAsync, log to LinePushLog
- [ ] T040 [US2] Implement `PushStatusChangedNotificationAsync` method - notify assigned user and reporter (if both have bindings)
- [ ] T041 [US2] Implement `PushAssignmentChangedNotificationAsync` method - notify new assigned user and reporter
- [ ] T042 [US2] Implement retry logic with exponential backoff in PushMessageAsync (max 3 retries: 1s, 2s, 4s delays)
- [ ] T043 [US2] Create LinePushLog entries for all push attempts (Success/Failed/Retrying status, RetryCount, ErrorMessage)
- [ ] T044 [US2] Integrate push notifications into `IssueReportService.CreateIssueAsync` - call PushNewIssueNotificationAsync after successful issue creation
- [ ] T045 [US2] Integrate push notifications into `IssueReportService.UpdateIssueAsync` - detect status/assignment changes, call appropriate notification methods
- [ ] T046 [US2] Add error handling to ensure push failures do not block issue creation/update (fail-safe design)
- [ ] T047 [US2] Add logging for all push operations (success/failure) using ILogger<LineMessagingService>

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - LINE 端直接回報問題 (Priority: P3)

**Goal**: 已綁定使用者可在 LINE 官方帳號中透過對話方式回報問題，系統引導填寫所有必要資訊（標題、內容、單位、緊急程度、聯絡人、電話），支援圖片上傳（最多3張），確認後自動建立回報單並同步至網頁系統

**Independent Test**: 已綁定使用者在 LINE 輸入「回報問題」，系統引導完成所有步驟（標題→內容→單位→緊急程度→聯絡人→電話→圖片→確認），點擊「確認送出」後，LINE 顯示回報單編號和查看連結，同時網頁系統中出現該回報單且資訊完整一致

### Tests for User Story 3 (OPTIONAL) ⚠️

- [ ] T048 [P] [US3] Create `LineWebhookControllerTests.cs` unit test in `Tests/ClarityDesk.UnitTests/` to test webhook signature validation and event routing
- [ ] T049 [P] [US3] Create `LineConversationFlowTests.cs` unit test in `Tests/ClarityDesk.UnitTests/` to test conversation state machine logic
- [ ] T050 [P] [US3] Create `LineWebhookIntegrationTests.cs` in `Tests/ClarityDesk.IntegrationTests/` to test complete conversation flow with database

### Implementation for User Story 3

- [ ] T051 [P] [US3] Create API Controller `Controllers/LineWebhookController.cs` with POST /api/line/webhook endpoint
- [ ] T052 [US3] Implement webhook signature validation in LineWebhookController using X-Line-Signature header and HMAC-SHA256 with Channel Secret
- [ ] T053 [US3] Implement event routing in webhook handler - parse LineMessageDto, route to HandleTextMessageAsync, HandleImageMessageAsync, HandlePostbackAsync based on event type
- [ ] T054 [US3] Return 200 OK immediately in webhook endpoint (within 3 seconds) to prevent LINE retry, process events in background if needed
- [ ] T055 [US3] Implement `HandleWebhookEventAsync` method in LineMessagingService - main event dispatcher
- [ ] T056 [US3] Implement `HandleTextMessageAsync` in LineMessagingService - check user binding, get/create conversation state, process text based on current step
- [ ] T057 [US3] Implement conversation flow for "回報問題" keyword - create new LineConversationState with CurrentStep = AskTitle, ExpiresAt = now + 24h
- [ ] T058 [US3] Implement `ProcessConversationStep` method - switch on CurrentStep, update state with user input, advance to next step, call ReplyMessageAsync with next question
- [ ] T059 [US3] Implement AskTitle step - save Title to conversation state, advance to AskContent
- [ ] T060 [US3] Implement AskContent step - save Content to conversation state, advance to AskDepartment, send quick reply with department options
- [ ] T061 [US3] Implement `HandlePostbackAsync` for department selection - parse "action=select_department&id={id}", update DepartmentId, advance to AskPriority
- [ ] T062 [US3] Implement AskPriority step with quick reply buttons - Low/Medium/High options, parse "action=select_priority&value={value}"
- [ ] T063 [US3] Implement AskCustomerName step - save CustomerName, advance to AskCustomerPhone
- [ ] T064 [US3] Implement AskCustomerPhone step - validate phone format, save CustomerPhone, advance to AskImages
- [ ] T065 [US3] Implement `HandleImageMessageAsync` in LineMessagingService - download image from LINE Content API, validate size/format, save to wwwroot/uploads/line-images/{tempId}/, store URL in ImageUrls JSON array
- [ ] T066 [US3] Implement image limit validation (max 3 images) - count ImageUrls array length, reject if >= 3
- [ ] T067 [US3] Implement "跳過" keyword handling in AskImages step - advance to Confirm step
- [ ] T068 [US3] Implement Confirm step - display summary Flex Message with all collected data, show "確認送出" and "取消" quick reply buttons
- [ ] T069 [US3] Implement `HandlePostbackAsync` for "action=confirm" - create IssueReport entity, auto-assign based on DepartmentId, move images to final location, create LinePushLog, advance to Completed
- [ ] T070 [US3] Implement `HandlePostbackAsync` for "action=cancel" - delete conversation state, reply with cancellation message
- [ ] T071 [US3] Implement "取消" keyword handling at any step - delete conversation state immediately
- [ ] T072 [US3] Implement timeout detection - check ExpiresAt on conversation state retrieval, delete if expired, reply with timeout message
- [ ] T073 [US3] Implement image download from LINE Content API - GET https://api-data.line.me/v2/bot/message/{messageId}/content with Authorization header
- [ ] T074 [US3] Implement auto-assignment logic - query DepartmentUser for default handler, fallback to admin if no default
- [ ] T075 [US3] Implement `ReplyMessageAsync` method in LineMessagingService - POST to https://api.line.me/v2/bot/message/reply with replyToken
- [ ] T076 [US3] Implement conversation state cleanup on successful issue creation - delete LineConversationState record
- [ ] T077 [US3] Add comprehensive error handling for all conversation steps (invalid input, API failures, database errors)
- [ ] T078 [US3] Add logging for all webhook events and conversation state transitions using ILogger<LineMessagingService>
- [ ] T079 [US3] Implement not-bound user handling - check LineBinding before starting conversation, reply with binding URL if not found

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and optional enhancements

- [ ] T080 [P] Create `ConversationCleanupService.cs` background service in `Infrastructure/BackgroundServices/` to clean expired LineConversationState records (runs every 1 hour)
- [ ] T081 Register ConversationCleanupService as IHostedService in `Program.cs` (OPTIONAL enhancement)
- [ ] T082 [P] Add comprehensive XML documentation comments to ILineMessagingService interface and all public methods
- [ ] T083 [P] Update `quickstart.md` validation - verify ngrok setup, webhook URL configuration, binding flow, push notification, conversation flow
- [ ] T084 Add monitoring queries to documentation - LinePushLog failure rate, conversation state metrics, image storage size
- [ ] T085 [P] Code review and refactoring - ensure all error messages are user-friendly, consistent naming conventions, DRY principle
- [ ] T086 Security review - verify webhook signature validation, input sanitization, image upload restrictions, rate limiting considerations
- [ ] T087 Performance optimization - add response caching for department list, optimize conversation state queries with proper indexes
- [ ] T088 [P] Create deployment checklist - HTTPS requirement, webhook URL update, Channel Secret/Token configuration, image directory permissions
- [ ] T089 Run all unit tests (if created): `dotnet test Tests/ClarityDesk.UnitTests/ClarityDesk.UnitTests.csproj`
- [ ] T090 Run all integration tests (if created): `dotnet test Tests/ClarityDesk.IntegrationTests/ClarityDesk.IntegrationTests.csproj`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1) - LINE 綁定管理**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2) - 推送通知**: Depends on User Story 1 binding data, but can be implemented independently once LineBinding table exists
- **User Story 3 (P3) - LINE 端回報**: Depends on User Story 1 binding data and User Story 2 LineMessagingService foundation, but delivers independent value

### Within Each User Story

- **User Story 1**:
  - Tests (optional) → PageModel → View → OAuth integration → Validation
- **User Story 2**:
  - Tests (optional) → Service foundation → Flex Message builders → Push methods → Integration into IssueReportService
- **User Story 3**:
  - Tests (optional) → Webhook controller → Event handlers → Conversation state machine → Image handling → Issue creation

### Parallel Opportunities

- **Phase 1**: All 3 setup tasks can run in parallel
- **Phase 2**:
  - T005-T006, T008, T010-T011, T015-T018 can all run in parallel (different files)
  - Entity configurations, DTOs, and enums are independent
- **User Story 1**: T022-T023 tests can run in parallel, T024-T025 view/model can be developed together
- **User Story 2**: T032-T033 tests in parallel, T036-T038 message builders in parallel
- **User Story 3**: T048-T050 tests in parallel, conversation step implementations can be parallelized
- **Phase 6**: T080, T082-T083, T085, T088-T090 can run in parallel (different concerns)

---

## Parallel Example: User Story 2

```bash
# Launch all tests for User Story 2 together (if tests requested):
Task T032: "Create LineMessagingServiceTests.cs unit test"
Task T033: "Create LinePushNotificationIntegrationTests.cs integration test"

# Launch all Flex Message builders in parallel:
Task T036: "Implement BuildNewIssueFlexMessage private method"
Task T037: "Implement BuildStatusChangedMessage private method"
Task T038: "Implement BuildAssignmentChangedFlexMessage private method"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. **Complete Phase 1**: Setup (3 tasks, ~30 minutes)
2. **Complete Phase 2**: Foundational (18 tasks, CRITICAL - ~4-6 hours)
3. **Complete Phase 3**: User Story 1 - LINE 綁定管理 (~4-6 hours)
4. **STOP and VALIDATE**:
   - Test binding flow end-to-end
   - Verify LINE Login OAuth works
   - Check database LineBinding records
   - Test unbind flow
5. **Deploy/Demo**: Users can now bind LINE accounts (foundational for all other features)

**Estimated MVP Time**: 1-2 days for experienced ASP.NET Core developer

### Incremental Delivery

1. **Foundation** (Phase 1 + 2): ~1 day → Database ready, services registered
2. **MVP** (+ Phase 3 - US1): ~1.5 days → Binding works, LINE accounts linked ✅
3. **Notifications** (+ Phase 4 - US2): ~2.5 days → Push notifications active ✅
4. **Full LINE Integration** (+ Phase 5 - US3): ~4-5 days → LINE-based reporting works ✅
5. **Polish** (+ Phase 6): ~5-6 days → Background cleanup, full testing, documentation ✅

**Total Estimated Time**: 5-6 days for full feature (single developer)

### Parallel Team Strategy

With 3 developers:

1. **Team completes Setup + Foundational together** (~1 day)
2. **Once Foundational is done**:
   - **Developer A**: User Story 1 (Binding) - Day 2
   - **Developer B**: User Story 2 (Push) - Day 2 (can start once LineBinding entity exists)
   - **Developer C**: User Story 3 (Conversation) - Day 2-3 (needs US2 LineMessagingService)
3. **Day 3**: Integration testing, bug fixes
4. **Day 4**: Polish phase tasks in parallel

**Total Estimated Time with 3 devs**: 3-4 days

---

## Task Count Summary

- **Phase 1 (Setup)**: 3 tasks
- **Phase 2 (Foundational)**: 18 tasks
- **Phase 3 (User Story 1)**: 10 tasks (2 optional tests + 8 implementation)
- **Phase 4 (User Story 2)**: 16 tasks (2 optional tests + 14 implementation)
- **Phase 5 (User Story 3)**: 31 tasks (3 optional tests + 28 implementation)
- **Phase 6 (Polish)**: 11 tasks

**Total**: 89 tasks (7 optional test tasks + 82 implementation/infrastructure tasks)

---

## Independent Test Criteria

### User Story 1 - LINE 綁定管理

✅ **Pass Criteria**:
- User can navigate to /Account/LineBinding page
- Clicking "綁定 LINE 官方帳號" initiates LINE Login OAuth flow
- After OAuth success, page shows "已綁定" with LINE display name
- LineBinding record exists in database with correct LineUserId
- Clicking "解除綁定" updates status to "未綁定"
- Guest accounts see error when attempting to bind
- Duplicate LINE account binding is prevented

### User Story 2 - 推送通知

✅ **Pass Criteria**:
- Creating new issue with assigned user (has LINE binding) triggers push notification
- LINE user receives Flex Message with all required fields (編號, 標題, 緊急程度, 單位, 聯絡人, 電話, 日期, 回報人)
- Clicking "查看詳細內容" button opens correct issue details page
- Changing issue status triggers notification to assigned user and reporter
- Changing assigned user triggers notification to new assignee and reporter
- Push failures are logged to LinePushLog with error details
- Push failures do NOT prevent issue creation/update (fail-safe)

### User Story 3 - LINE 端回報

✅ **Pass Criteria**:
- User sends "回報問題" in LINE, bot starts conversation
- Bot guides through all steps: 標題 → 內容 → 單位 → 緊急程度 → 聯絡人 → 電話 → 圖片 → 確認
- User can upload up to 3 images, system rejects 4th image
- User can type "跳過" to skip image upload
- Confirmation shows all collected data accurately
- Clicking "確認送出" creates IssueReport in database
- Bot replies with issue number and details URL
- Issue appears in web system with all data including images
- User can cancel anytime by typing "取消"
- Conversation expires after 24 hours with proper cleanup

---

## Notes

- **[P] tasks**: Different files, no dependencies - can run in parallel
- **[Story] label**: Maps task to specific user story for traceability
- **Tests are OPTIONAL**: Only implement if time permits or TDD approach requested
- **Each user story should be independently completable and testable**
- **Verify tests fail before implementing** (if writing tests)
- **Commit after each task or logical group**
- **Stop at any checkpoint to validate story independently**
- **Constitution compliance**: All new files, minimal changes to existing files (Program.cs, appsettings.json, ApplicationDbContext.cs)
- **No new NuGet packages**: Uses .NET 8 built-in HttpClient and System.Text.Json

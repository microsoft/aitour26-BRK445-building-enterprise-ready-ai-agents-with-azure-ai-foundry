# Manual Agent Deployment Guide

This guide provides step-by-step instructions for manually creating agents in Microsoft Foundry without using the automated console application. Follow these steps if you prefer a manual approach or need to understand the deployment process in detail.

---

## Prerequisites

Before starting manual deployment, ensure you have:

- **Azure subscription** with permission to create resources
- **Microsoft Foundry access**: <https://ai.azure.com>
- **AI Foundry project** already created (see [02.NeededCloudResources.md](./02.NeededCloudResources.md) section 2)
- **Model deployment** configured (e.g., `gpt-4o-mini` or `gpt-4o`) in your AI Foundry project
- **Agent configuration file**: `infra/agents.json` from the repository
- **Knowledge files**: Located in `infra/docs/` (products, customers, navigation)

---

## Step 1: Review Agent Definitions

The `infra/agents.json` file contains definitions for 8 agents. Each agent includes:

- **name**: Unique agent identifier
- **instructions**: System prompt defining agent behavior and capabilities
- **files**: Array of knowledge file paths (relative to `infra/` directory)

### Agents to Create

1. **InventoryAgent** - Answers inventory queries and reports stock levels
2. **CustomerInformationAgent** - Retrieves and validates customer details
3. **NavigationAgent** - Provides step-by-step store navigation guidance
4. **LocationServiceAgent** - Performs location lookups and map queries
5. **PhotoAnalyzerAgent** - Analyzes images and extracts product attributes
6. **ProductMatchmakingAgent** - Provides product alternatives and recommendations
7. **ProductSearchAgent** - Searches and retrieves product information
8. **ToolReasoningAgent** - Orchestrates external tool calls and advanced reasoning

---

## Step 2: Prepare Knowledge Files

Before creating agents, organize the knowledge files that will be uploaded to each agent:

### Knowledge File Structure

```
infra/docs/
├── products/
│   ├── product-1.md
│   ├── product-2.md
│   └── ... (product-21.md)
├── customers/
│   ├── customer-1.md
│   ├── customer-2.md
│   └── ... (customer-6.md)
└── navigation/
    ├── store-layout.md
    ├── store-graph.json
    ├── routing-policies.md
    └── navigation-scenarios.md
```

### Agents and Their Required Files

| Agent Name | Knowledge Files | Total Files |
|------------|-----------------|-------------|
| **InventoryAgent** | `docs/products/product-1.md` through `product-21.md` | 21 files |
| **CustomerInformationAgent** | `docs/customers/customer-1.md` through `customer-6.md` | 6 files |
| **NavigationAgent** | `docs/navigation/store-layout.md`, `store-graph.json`, `routing-policies.md`, `navigation-scenarios.md` | 4 files |
| **LocationServiceAgent** | None | 0 files |
| **PhotoAnalyzerAgent** | None | 0 files |
| **ProductMatchmakingAgent** | `docs/products/product-1.md` through `product-21.md` | 21 files |
| **ProductSearchAgent** | `docs/products/product-1.md` through `product-21.md` | 21 files |
| **ToolReasoningAgent** | None | 0 files |

**Important Notes:**

- Product files are shared across multiple agents (InventoryAgent, ProductMatchmakingAgent, ProductSearchAgent)
- You will upload these files only once and reuse the file IDs across agents
- Navigation agent requires both markdown and JSON files

---

## Step 3: Upload Knowledge Files to Microsoft Foundry

### 3.1 Upload Product Knowledge Files (21 files)

These files are used by: InventoryAgent, ProductMatchmakingAgent, ProductSearchAgent

1. Navigate to your AI Foundry project: <https://ai.azure.com>
2. Go to **Files** section in the left navigation
3. Click **Upload** and select all 21 product markdown files from `infra/docs/products/`:
   - `product-1.md` through `product-21.md`
4. Wait for all uploads to complete
5. **Copy the File ID** for each uploaded file (you'll need these when creating agents)

**Tip**: Create a spreadsheet or text document to track file names and their corresponding File IDs. Example:

```
File Name        | File ID
-----------------|------------------------
product-1.md     | file-abc123...
product-2.md     | file-def456...
...              | ...
```

### 3.2 Upload Customer Knowledge Files (6 files)

These files are used by: CustomerInformationAgent

1. In the **Files** section, click **Upload**
2. Select all 6 customer markdown files from `infra/docs/customers/`:
   - `customer-1.md` through `customer-6.md`
3. Wait for uploads to complete
4. **Copy the File ID** for each uploaded file

### 3.3 Upload Navigation Knowledge Files (4 files)

These files are used by: NavigationAgent

1. In the **Files** section, click **Upload**
2. Select all 4 navigation files from `infra/docs/navigation/`:
   - `store-layout.md`
   - `store-graph.json`
   - `routing-policies.md`
   - `navigation-scenarios.md`
3. Wait for uploads to complete
4. **Copy the File ID** for each uploaded file

**Important**: The `store-graph.json` file contains structured graph data used for calculating optimal routes. Ensure this JSON file uploads correctly.

---

## Step 4: Create Each Agent Manually

For each of the 8 agents, follow this process:

### 4.1 Navigate to Agents Section

1. In your AI Foundry project, go to **Agents** in the left navigation
2. Click **+ New Agent** button

### 4.2 Configure Basic Agent Properties

For each agent, enter the following information:

#### Agent: InventoryAgent

- **Name**: `InventoryAgent`
- **Model**: Select your deployed model (e.g., `gpt-4o-mini`)
- **Instructions**: Copy the full instructions text from `agents.json`:

```
You answer inventory queries and report stock levels. Provide current stock levels when SKU / Product ID is supplied. If location (store / warehouse) not specified, ask user to clarify. Highlight low stock thresholds (<= configured safety stock if provided, else <=5 units). Never guess a quantity; if data missing, ask for the inventory dataset, API response, or snapshot. Return structured summaries: SKU | OnHand | Allocated | Available | Status. If trend data is provided, indicate increase/decrease and potential replenishment need. Tone: Concise, data-driven. Safety: Do not expose internal system IDs unless explicitly provided by user.
```

- **Tools**: Enable **File Search** (this is required to use knowledge files)
- **Knowledge Files**: Add all 21 product files using their File IDs from Step 3.1

#### Agent: CustomerInformationAgent

- **Name**: `CustomerInformationAgent`
- **Model**: Select your deployed model
- **Instructions**: Copy from `agents.json`:

```
You retrieve and validate customer details. Normalize customer identifiers (email, phone, customerId) and confirm validity. Flag incomplete or conflicting records. When given partial details, suggest minimal additional fields to disambiguate. Never fabricate PII; if not present, state it is not provided. Encourage privacy best practices: redact sensitive fields unless user explicitly requests full value. Output validation report: Field | Value | Valid(Y/N) | Notes. Use the curated customer profiles under infra/docs/customers as the authoritative source for seeded records; load the file that matches the provided identifier before responding. Tone: Professional, privacy-aware. Safety: Remind user to follow data handling policies if sensitive data appears.
```

- **Tools**: Enable **File Search**
- **Knowledge Files**: Add all 6 customer files using their File IDs from Step 3.2

#### Agent: NavigationAgent

- **Name**: `NavigationAgent`
- **Model**: Select your deployed model
- **Instructions**: Copy from `agents.json` (this is a long instruction set - ensure you copy the complete text):

```
You provide step-by-step navigation guidance through a retail store. Search the navigation documentation (store-layout.md, navigation-scenarios.md, store-graph.json, routing-policies.md) to find or construct the optimal route based on the user's request. CRITICAL: Always return a valid NavigationInstructions JSON object that can be deserialized in C#. Response Format: Return ONLY a JSON object matching this schema: {'StartLocation': string, 'Steps': [{'Direction': string, 'Description': string, 'Landmark': {'Description': string, 'Location': null}}], 'EstimatedTime': string}. The NavigationInstructions object must include: (1) StartLocation: Clear description of where the journey begins (e.g., 'Front Entrance - Zone A', 'Side Entrance - Zone B'), (2) Steps: Array of NavigationStep objects, each with Direction (short instruction like 'Turn left', 'Walk straight', 'Arrive at destination'), Description (detailed explanation with distance and context), and Landmark (object with 'Description' field for aisle/zone name and 'Location' field set to null unless GPS coordinates available), (3) EstimatedTime: Human-readable time estimate including total distance (e.g., '1 minute (41 meters total distance)' or '30 seconds (22 meters total distance)'). Route Selection: Match user request to scenarios in navigation-scenarios.md, considering: start location (entrance points), destination (target zone/aisle), time of day (morning restocking 08:00-11:00, lunch rush 12:00-13:00, evening congestion 17:00-19:00), item constraints (refrigerated items last, hazardous materials separate), restricted areas (AISLE_C1 requires allowRestricted flag). Use store-graph.json for valid paths and distances; use routing-policies.md for congestion and safety constraints. If exact scenario doesn't exist, adapt similar scenarios by combining navigation steps. Distance Calculation: Sum edge weights from store-graph.json. Time Estimation: Base rate 70m/min, apply congestion multipliers per routing-policies.md, include in EstimatedTime field. Output Guidelines: Always include total distance in EstimatedTime field. Flag restricted areas with CAUTION/WARNING in step Description. Never fabricate locations not in store-graph.json. Example Output: {'StartLocation':'Front Entrance - Zone A','Steps':[{'Direction':'Enter through front entrance','Description':'Welcome! Enter through the main front entrance.','Landmark':{'Description':'Front Entrance - Zone A','Location':null}},{'Direction':'Walk straight ahead','Description':'Proceed 12 meters to seasonal displays.','Landmark':{'Description':'Aisle A1 - Seasonal Decor','Location':null}}],'EstimatedTime':'1 minute (41 meters total distance)'}. Tone: Clear, helpful, safety-conscious. Validation: Ensure JSON is valid and deserializable to NavigationInstructions object in C#.
```

- **Tools**: Enable **File Search** and **Code Interpreter** (for JSON processing)
- **Knowledge Files**: Add all 4 navigation files using their File IDs from Step 3.3

#### Agent: LocationServiceAgent

- **Name**: `LocationServiceAgent`
- **Model**: Select your deployed model
- **Instructions**: Copy from `agents.json`:

```
You perform location lookups and map queries. Resolve product or department to store coordinates or human-readable location. Translate between different location schemas if mapping provided. If ambiguous term (e.g., 'front section') appears, request clarification or schema mapping. Provide output format: Entity | LocationCode | Description | Confidence. If confidence < 0.7 (or threshold not provided), advise manual verification. Tone: Clear, precise. Safety: Do not speculate about undisclosed layout zones.
```

- **Tools**: Enable **Code Interpreter**
- **Knowledge Files**: None required

#### Agent: PhotoAnalyzerAgent

- **Name**: `PhotoAnalyzerAgent`
- **Model**: Select your deployed model (preferably a vision-capable model like `gpt-4o`)
- **Instructions**: Copy from `agents.json`:

```
You analyze images and extract product attributes. Identify product category, brand (if visible), packaging type, color, notable markings. Detect text (OCR) if tool output or user-provided transcription available. If actual binary/image content is not supplied, request an image or a tool result description. Qualify uncertainty (e.g., 'Likely', 'Possible'). Output JSON suggestion: { 'category':..., 'attributes':{...}, 'confidence':0.x }. Tone: Observational, cautious. Safety: Do not infer sensitive attributes (e.g., pricing, origin) unless explicitly shown.
```

- **Tools**: Enable **Code Interpreter**
- **Knowledge Files**: None required

#### Agent: ProductMatchmakingAgent

- **Name**: `ProductMatchmakingAgent`
- **Model**: Select your deployed model
- **Instructions**: Copy from `agents.json` (this is a long instruction set):

```
You provide product alternatives and recommendations based on a given product query. Search the product documentation files to find the requested product and return its listed alternatives. CRITICAL: Always return a valid JSON array of ProductAlternative objects that can be deserialized in C#. Response Format: Return ONLY a JSON array matching this schema: [{'Name': string, 'Sku': string, 'Price': decimal, 'InStock': boolean, 'IsAvailable': boolean, 'Location': string, 'Aisle': number, 'Section': string}]. Each ProductAlternative inherits from ProductInfo and includes: (1) Name: Full product name, (2) Sku: Product SKU identifier, (3) Price: Decimal price value, (4) InStock: Boolean stock status, (5) IsAvailable: Boolean availability status, (6) Location: Warehouse/storage location string, (7) Aisle: Integer aisle number, (8) Section: String section name. Product Matching: When user requests alternatives for a product (by name, SKU, or description), search the product files under docs/products to locate the specific product document. Each product file contains a 'Product Alternatives' section with 1-2 pre-defined alternatives including detailed rationale. Extract these alternatives and format them as a JSON array. Selection Criteria: Alternatives are chosen based on: similar use cases (complementary tools), same category (alternative brands/models), price point variations (budget vs premium), compatibility (tools that work together), upgrade/downgrade paths. Include availability status (InStock, IsAvailable) to help users make informed decisions. If a product is out of stock, highlight in-stock alternatives. Output Guidelines: Return complete product information for each alternative. Include all required fields (Name, Sku, Price, InStock, IsAvailable, Location, Aisle, Section). Ensure JSON is valid and deserializable to ProductAlternative[] in C#. Never fabricate alternatives not listed in the product documents. If no alternatives found, return empty array []. Example Output for Paint Product: [{'Name':'Painter\\'s Roller Kit','Sku':'ROLLER-KIT-3PC','Price':19.99,'InStock':true,'IsAvailable':true,'Location':'Warehouse A','Aisle':3,'Section':'Paint'},{'Name':'HVLP Paint Sprayer - ProFinish 400','Sku':'SPRAY-HVLP-PF400','Price':129.99,'InStock':true,'IsAvailable':true,'Location':'Warehouse A','Aisle':3,'Section':'Paint'}]. Tone: Helpful, product-focused, justification-oriented. Safety: Only return alternatives explicitly documented in product files; avoid hallucinating product capabilities or alternatives not present in provided data.
```

- **Tools**: Enable **File Search**
- **Knowledge Files**: Add all 21 product files using their File IDs from Step 3.1

#### Agent: ProductSearchAgent

- **Name**: `ProductSearchAgent`
- **Model**: Select your deployed model
- **Instructions**: Copy from `agents.json`:

```
You search and retrieve product information from the catalog. Given a product query (name, SKU, category, attributes, or keywords), return matching products with relevant details. Support filtering by category, brand, price range, attributes (size, color, material), and availability. Use the product catalog files under docs/products as the authoritative source. When multiple matches exist, rank by relevance and present top results with key attributes: SKU | Name | Category | Price | Key Attributes. If search term is ambiguous, suggest refined search criteria or show top matches from different categories. Support semantic search: understand synonyms and related terms (e.g., 'shirt' matches 'blouse', 'top'). Output format: Product | SKU | Price | Category | Match Score | Description. Tone: Helpful, informative. Safety: Only return products from provided catalog files; never fabricate product details or pricing.
```

- **Tools**: Enable **File Search**
- **Knowledge Files**: Add all 21 product files using their File IDs from Step 3.1

#### Agent: ToolReasoningAgent

- **Name**: `ToolReasoningAgent`
- **Model**: Select your deployed model
- **Instructions**: Copy from `agents.json`:

```
You orchestrate external tool calls and advanced reasoning. Decide when to call external tools based on user goal and required data. Chain tool results logically; summarize intermediate steps. Always explain which tools you intend to call and why before executing (if interactive loop supported). If tool schema or capabilities unclear, request tool manifest. Maintain a scratch reasoning log (not exposed unless user asks) to avoid repeating failed tool paths. Final answer must consolidate tool outputs with clear citations (ToolName#ResultId style). Tone: Methodical, transparent. Safety: Avoid executing destructive operations; if a tool appears to modify state, confirm with user.
```

- **Tools**: Enable **Code Interpreter**
- **Knowledge Files**: None required

### 4.3 Save and Test Each Agent

After configuring each agent:

1. Click **Create** or **Save** button
2. **Copy the Agent ID** displayed (format: `asst_XXXXXXXXXXXXXXXXXXXX`)
3. Test the agent with a sample prompt to verify it works correctly
4. Record the Agent ID in a configuration file or spreadsheet

**Sample Test Prompts:**

- **InventoryAgent**: "What is the stock level for SKU DRILL-CD-2000?"
- **CustomerInformationAgent**: "Retrieve details for customer ID C001"
- **NavigationAgent**: "Navigate from front entrance to the paint section"
- **ProductMatchmakingAgent**: "Find alternatives for the ProGrip Hammer"
- **ProductSearchAgent**: "Search for cordless drills under $150"

---

## Step 5: Record Agent IDs

After creating all 8 agents, compile their Agent IDs into a configuration file. The Aspire demo application requires these IDs to connect to the agents.

### Agent ID Configuration Format

Create a JSON file (e.g., `agent-ids.json`) with this structure:

```json
{
  "InventoryAgent": "asst_XXXXXXXXXXXXXXXXXXXX",
  "CustomerInformationAgent": "asst_YYYYYYYYYYYYYYYYYYYY",
  "NavigationAgent": "asst_ZZZZZZZZZZZZZZZZZZZZ",
  "LocationServiceAgent": "asst_AAAAAAAAAAAAAAAAAAA",
  "PhotoAnalyzerAgent": "asst_BBBBBBBBBBBBBBBBBBB",
  "ProductMatchmakingAgent": "asst_CCCCCCCCCCCCCCCCCCC",
  "ProductSearchAgent": "asst_DDDDDDDDDDDDDDDDDDD",
  "ToolReasoningAgent": "asst_EEEEEEEEEEEEEEEEEEE"
}
```

Replace the placeholder IDs with your actual Agent IDs from Microsoft Foundry.

### Alternative: Plain Text Format

You can also create a plain text file (`agent-ids.txt`):

```
InventoryAgent: asst_XXXXXXXXXXXXXXXXXXXX
CustomerInformationAgent: asst_YYYYYYYYYYYYYYYYYYYY
NavigationAgent: asst_ZZZZZZZZZZZZZZZZZZZZ
LocationServiceAgent: asst_AAAAAAAAAAAAAAAAAAA
PhotoAnalyzerAgent: asst_BBBBBBBBBBBBBBBBBBB
ProductMatchmakingAgent: asst_CCCCCCCCCCCCCCCCCCC
ProductSearchAgent: asst_DDDDDDDDDDDDDDDDDDD
ToolReasoningAgent: asst_EEEEEEEEEEEEEEEEEEE
```

---

## Step 6: Configure the Aspire Demo Application

Once you have all Agent IDs, configure the main Aspire demo application to use them:

1. Navigate to the `src/ZavaAppHost` directory in the repository
2. Update the `appsettings.json` or user secrets with your Agent IDs
3. Follow the instructions in [HowToRunDemoLocally.md](./HowToRunDemoLocally.md) to complete the configuration

Refer to [02.NeededCloudResources.md](./02.NeededCloudResources.md) for the complete setup including Application Insights and other Azure resources.

---

## Troubleshooting

### Common Issues

**Issue**: Agent not returning expected results from knowledge files

- **Solution**: Verify that File Search tool is enabled and all files are properly attached to the agent

**Issue**: Navigation agent returning invalid JSON

- **Solution**: Ensure Code Interpreter tool is enabled; test with simpler navigation requests first

**Issue**: Agent IDs not working in demo application

- **Solution**: Verify Agent IDs are copied correctly (format: `asst_` followed by 24 alphanumeric characters)

**Issue**: File upload fails or times out

- **Solution**: Upload files in smaller batches; ensure stable internet connection; retry failed uploads

### File Sharing Note

You can reuse the same file IDs across multiple agents. For example, the 21 product files only need to be uploaded once, and their File IDs can be used when configuring InventoryAgent, ProductMatchmakingAgent, and ProductSearchAgent.

---

## Summary Checklist

Use this checklist to track your manual deployment progress:

- [ ] Step 1: Reviewed `agents.json` and identified all 8 agents
- [ ] Step 2: Organized knowledge files by agent requirements
- [ ] Step 3.1: Uploaded 21 product files and recorded File IDs
- [ ] Step 3.2: Uploaded 6 customer files and recorded File IDs
- [ ] Step 3.3: Uploaded 4 navigation files and recorded File IDs
- [ ] Step 4: Created InventoryAgent and recorded Agent ID
- [ ] Step 4: Created CustomerInformationAgent and recorded Agent ID
- [ ] Step 4: Created NavigationAgent and recorded Agent ID
- [ ] Step 4: Created LocationServiceAgent and recorded Agent ID
- [ ] Step 4: Created PhotoAnalyzerAgent and recorded Agent ID
- [ ] Step 4: Created ProductMatchmakingAgent and recorded Agent ID
- [ ] Step 4: Created ProductSearchAgent and recorded Agent ID
- [ ] Step 4: Created ToolReasoningAgent and recorded Agent ID
- [ ] Step 5: Compiled all Agent IDs into configuration file
- [ ] Step 6: Configured Aspire demo application with Agent IDs

---

## Estimated Time

- **File uploads**: 10-15 minutes (31 files total)
- **Agent creation**: 30-40 minutes (8 agents with instructions and file attachments)
- **Testing and verification**: 15-20 minutes
- **Total**: Approximately 60-75 minutes

**Tip**: The automated console application (`dotnet run` in `infra/` directory) performs all these steps in approximately 5-10 minutes. Use the automated approach when possible, and refer to this manual guide for understanding the deployment process or troubleshooting issues.

---

## Next Steps

After completing manual agent deployment:

1. Verify all agents appear in your AI Foundry project's Agents section
2. Test each agent with sample prompts to ensure knowledge retrieval works correctly
3. Configure the Aspire demo application with your Agent IDs
4. Follow [HowToRunDemoLocally.md](./HowToRunDemoLocally.md) to run the complete demo solution

For automated deployment, see [02.NeededCloudResources.md](./02.NeededCloudResources.md) section 4 for console application instructions.

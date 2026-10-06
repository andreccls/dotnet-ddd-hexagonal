# Works with or without a local .NET SDK:
#  - `dotnet` found  -> runs natively (MySQL still comes from docker compose)
#  - `dotnet` absent -> runs inside the official SDK container (docker compose, service `sdk`)
-include .env
export

MYSQL_PORT ?= 3306
MYSQL_ROOT_PASSWORD ?= dev_only_root_password
COMPOSE := docker compose

ifneq ($(shell command -v dotnet 2>/dev/null),)
  # Native mode. The test projects read this variable to find MySQL (no Database=, each test class creates its own).
  export TEST_MYSQL_CONNECTION ?= Server=127.0.0.1;Port=$(MYSQL_PORT);User=root;Password=$(MYSQL_ROOT_PASSWORD)
  RUN       =
  RUN_NODB  =
  DB_DEP    = up-db
else
  RUN       = $(COMPOSE) --profile tools run --rm sdk
  RUN_NODB  = $(COMPOSE) --profile tools run --rm --no-deps sdk
  DB_DEP    =
endif

.DEFAULT_GOAL := help
.PHONY: help build test coverage up down up-db clean migration

help: ## Show this help
	@grep -E '^[a-z-]+:.*##' $(MAKEFILE_LIST) | awk -F':.*## ' '{printf "  make %-10s %s\n", $$1, $$2}'

build: ## Compile everything (warnings are errors)
	$(RUN_NODB) dotnet build DddHexagonal.sln -warnaserror

test: build $(DB_DEP) ## Run ALL tests (needs MySQL: started automatically)
	$(RUN) dotnet test DddHexagonal.sln --no-build

coverage: $(DB_DEP) ## Tests + coverage. Fails if Domain/Application < 100%. Report in coverage/report
	$(RUN) ./scripts/coverage.sh

up: ## Start API + MySQL (http://localhost:8080/swagger)
	$(COMPOSE) up --build -d --wait

down: ## Stop the stack (keeps the database volume)
	$(COMPOSE) down

up-db: ## Start only MySQL (used by the integration tests)
	$(COMPOSE) up -d --wait mysql

clean: ## Stop the stack, DELETE the volumes and build output
	$(COMPOSE) --profile tools down -v
	rm -rf coverage TestResults
	find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

migration: ## Create an EF migration: make migration NAME=AddSomething
	$(RUN_NODB) sh -c "dotnet tool restore && dotnet ef migrations add $(NAME) -p src/DddHexagonal.Infrastructure -s src/DddHexagonal.Infrastructure -o Persistence/Migrations"

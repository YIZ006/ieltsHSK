DIST_DIR := dist
ARTIFACT := $(DIST_DIR)/repository-snapshot.tar.gz

.PHONY: build clean

build:
	@mkdir -p $(DIST_DIR)
	@tar --exclude='./.git' --exclude='./$(DIST_DIR)' -czf $(ARTIFACT) .
	@echo "Built $(ARTIFACT)"

clean:
	@rm -rf $(DIST_DIR)

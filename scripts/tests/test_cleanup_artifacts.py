import importlib.util
from pathlib import Path
import unittest

SPEC = importlib.util.spec_from_file_location("cleanup", Path(__file__).parents[1] / "cleanup_artifacts.py")
cleanup = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(cleanup)


class CleanupTests(unittest.TestCase):
    def setUp(self):
        self.current = {"id": 10, "head_branch": "arena/test", "workflow_id": 20, "repository": {"id": 30}}
        self.previous = {**self.current, "id": 9, "event": "push"}
        self.artifact = {"name": "F8Translate-win-x64", "workflow_run": {"id": 9}}

    def test_removes_only_previous_matching_deliveries(self):
        self.assertTrue(cleanup.belongs_to_previous_delivery(self.artifact, self.current, self.previous))

    def test_preserves_current_other_workflows_branches_repositories_and_events(self):
        for change in [{"head_branch": "main"}, {"workflow_id": 99}, {"repository": {"id": 99}}, {"event": "pull_request"}]:
            with self.subTest(change=change):
                self.assertFalse(cleanup.belongs_to_previous_delivery(self.artifact, self.current, {**self.previous, **change}))
        self.assertFalse(cleanup.belongs_to_previous_delivery({**self.artifact, "workflow_run": {"id": 10}}, self.current, self.previous))
        self.assertFalse(cleanup.belongs_to_previous_delivery({**self.artifact, "name": "outro-projeto"}, self.current, self.previous))


if __name__ == "__main__":
    unittest.main()

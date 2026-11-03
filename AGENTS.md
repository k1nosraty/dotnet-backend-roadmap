# Course editing

For Persian lesson writing or editing, read `handbook/persian-writing.md` first.
If the `persian-writing` skill is available, use its prose and educational-content
guidance as well. The repository guide must remain usable without that skill.

Write for a learner who has completed the prerequisites but does not yet know
the new concept. Explain specialist terms on first use, connect cause and result,
and trace a concrete example. Do not shorten explanations into technical notes.
Keep identifiers, commands and actual output exact. Preserve technical limits
and validate explanations against the sample belonging to that lesson.
Run automated text cleanup on temporary copies, protect Markdown link targets,
and verify code blocks and targets remain unchanged before accepting its output.

For Markdown changes, run `python3 scripts/check_content.py` and
`git diff --check`. If code, commands or observable behavior change, run the
relevant checks documented by that section. A prose linter does not establish
technical correctness or learner comprehension.
